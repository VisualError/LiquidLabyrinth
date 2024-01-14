using UnityEngine;
using Color = UnityEngine.Color;
using Vector3 = UnityEngine.Vector3;

namespace LiquidLabyrinth.Labyrinth.Monobehaviours
{
    internal class LiquidContainerController : MonoBehaviour
    {
        private Color color;
        public BoxCollider Container;
        public float FillPercentage;
        private Color _lighterColor;

        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
                if (properties != null)
                {
                    _lighterColor = new Color(Mathf.Clamp01(value.r + 0.5f),
                    Mathf.Clamp01(value.g + 0.5f),
                    Mathf.Clamp01(value.b + 0.5f));
                    properties.SetColor("_LiquidColor", value);
                    properties.SetColor("_SurfaceColor", _lighterColor);
                }
            }
        }
        Vector3 center;
        Vector3 size;

        void Awake()
        {
            renderComponent = GetComponent<Renderer>();
        }

        void Start()
        {
            center = Container.center;
            size = Container.size;
        }

        private bool NoNeedToSimulate
        {
            get
            {
                return Mathf.Approximately(FillPercentage, 0f) || Mathf.Approximately(FillPercentage, 1f);
            }
        }

        private void OnWillRenderObject()
        {
            _lighterColor = new Color(Mathf.Clamp01(Color.r + 0.5f),
                    Mathf.Clamp01(Color.g + 0.5f),
                    Mathf.Clamp01(Color.b + 0.5f));
            if (!renderComponent || properties == null)
            {
                properties = new MaterialPropertyBlock();
                properties.SetColor("_LiquidColor", Color);
                properties.SetColor("_SurfaceColor", _lighterColor);
            }
            properties.SetColor("_LiquidColor", Color);
            properties.SetColor("_SurfaceColor", _lighterColor);
            properties.SetFloat("_Fill", FillPercentage);
            //properties.SetVector("_UvBounds", new Vector4(center.x + size.x / 2, center.y + size.y / 2, center.z + size.z / 2, 0));
            renderComponent.SetPropertyBlock(properties);

            if (NoNeedToSimulate) return;
            // funny test down here:
            //hehehehheheheehehehehheeeeeeeeee
            time += Time.deltaTime;
            // decrease wobble over time
            wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * Recovery);
            wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * Recovery);

            // make a sine wave of the decreasing wobble
            pulse = 2 * Mathf.PI * WobbleSpeed;
            wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
            wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

            // send it to the shader
            renderComponent.material.SetFloat("_WobbleX", wobbleAmountX);
            renderComponent.material.SetFloat("_WobbleZ", wobbleAmountZ);

            // velocity
            velocity = (lastPos - transform.position) / Time.deltaTime;
            angularVelocity = transform.rotation.eulerAngles - lastRot;


            // add clamped velocity to wobble
            wobbleAmountToAddX += Mathf.Clamp((velocity.x + (angularVelocity.z * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
            wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (angularVelocity.x * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);

            // keep last position
            lastPos = transform.position;
            lastRot = transform.rotation.eulerAngles;
        }

        float wobbleAmountX;
        float wobbleAmountZ;
        float wobbleAmountToAddX;
        float wobbleAmountToAddZ;
        float pulse;
        float time = 0.5f;
        Vector3 lastPos;
        Vector3 velocity;
        Vector3 lastRot;
        Vector3 angularVelocity;
        public float MaxWobble = 0.12f;
        //private float MaxWobbleBase = 1f;
        public float WobbleSpeed = 1f;
        public float Recovery = 1f;

        private MaterialPropertyBlock properties;
        internal Renderer renderComponent;
    }
}
