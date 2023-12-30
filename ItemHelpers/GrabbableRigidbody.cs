using LiquidLabyrinth.Utilities;
using UnityEngine;
namespace LiquidLabyrinth.ItemHelpers
{
    [RequireComponent(typeof(Rigidbody))]

    // Taken from: https://gist.github.com/EvaisaDev/aaf727b2aeb6733793c89a887f8f8615
    class GrabbableRigidbody : GrabbableObject
    {
        public float gravity = 9.8f;
        internal Rigidbody rb;
        public AudioSource itemAudio;
        public override void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            // force some properties which might be missconfigured
            itemProperties.itemSpawnsOnGround = false;
            base.Start();
            EnablePhysics(true);
            itemAudio = gameObject.GetComponent<AudioSource>();
        }

        public new void EnablePhysics(bool enable)
        {
            for (int i = 0; i < propColliders.Length; i++)
            {
                if (!(propColliders[i] == null) && !propColliders[i].gameObject.CompareTag("InteractTrigger") && !propColliders[i].gameObject.CompareTag("DoNotSet"))
                {
                    propColliders[i].enabled = enable;
                }
            }

            // enable rigidbody
            rb.isKinematic = !enable;
        }

        public override void Update()
        {
            // hax
            fallTime = 1.0f;
            reachedFloorTarget = true;
            var wasHeld = isHeld;
            // hella hax
            isHeld = true;
            base.Update();
            isHeld = wasHeld;
        }


        public virtual void FixedUpdate()
        {
            // handle gravity if rigidbody is enabled
            if (IsHost)
            {
                if (!rb.isKinematic && !isHeld)
                {
                    rb.useGravity = false;

                    rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
                }
                else
                {
                    rb.AddForce(Vector3.zero, ForceMode.VelocityChange);
                }
            }
        }

        public override void LateUpdate()
        {
            if (parentObject != null && isHeld)
            {
                transform.rotation = parentObject.rotation;
                transform.Rotate(itemProperties.rotationOffset);
                transform.position = parentObject.position;
                Vector3 positionOffset = itemProperties.positionOffset;
                positionOffset = parentObject.rotation * positionOffset;
                transform.position += positionOffset;
            }
            if (radarIcon != null)
            {
                radarIcon.position = transform.position;
            }
        }

        public virtual void OnCollisionEnter(Collision collision)
        {
            rb.isKinematic = false;
            float rigidBodyMangintude = rb.velocity.magnitude;
            if (collision.gameObject.tag == "Player") 
            {
                // this aint workin chief.
                Rigidbody playerRb = collision.gameObject.GetComponentInParent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 torqueDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                    float torqueAmount = 10f; // Adjust this value to change the amount of rotation
                    rb.AddTorque(torqueDirection * torqueAmount);
                }
                return;
            }
            float pitch = OtherUtils.mapValue(rigidBodyMangintude, 0.8f, 10f, 0.8f, 1.5f);
            itemAudio.pitch = pitch;
            itemAudio.PlayOneShot(itemProperties.dropSFX);
        }

        public override void FallWithCurve()
        {
            // stub, we do not need this.
        }

        public void FallToGround()
        {
            // stub, we do not need this.
        }

        public override void EquipItem()
        {
            // remove parent object
            base.EquipItem();
            itemAudio.pitch = 1f;
            transform.parent = null;
        }

        public override void InteractItem()
        {
            base.InteractItem();
            itemAudio.pitch = 1f;
        }
    }
}
