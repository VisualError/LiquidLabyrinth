using LethalLib;
using LiquidLabyrinth.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LiquidLabyrinth.ItemHelpers
{
    class PotionBottle : Throwable
    {

        Renderer rend;
        Vector3 lastPos;
        Vector3 velocity;
        Vector3 lastRot;
        Vector3 angularVelocity;
        public float MaxWobble = 1f;
        public float WobbleSpeed = 1f;
        public float Recovery = 1f;
        public float Fill = -1f;
        float wobbleAmountX;
        float wobbleAmountZ;
        float wobbleAmountToAddX;
        float wobbleAmountToAddZ;
        float pulse;
        float time = 0.5f;
        public GameObject Liquid;
        public List<string> BottleProperties; // TODO: Add bottle properties class. API in mind, change `string` into the class.
        string bottleType; // for debugging only
        private Dictionary<float, string> data = null;

        public NetworkVariable<Color> color = new NetworkVariable<Color>(Color.red, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<Color> lighterColor = new NetworkVariable<Color>(Color.blue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void Start()
        {
            LiquidLabyrinthBase.Logger.LogWarning("Start was called");
            base.Start();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsHost || IsServer)
            {
                LiquidLabyrinthBase.Logger.LogWarning("Client RPC called!");
                scrapValue = itemProperties.creditsWorth;
                ScanNodeProperties nodeProperties = GetComponentInChildren<ScanNodeProperties>();
                if (nodeProperties != null)
                {
                    if (nodeProperties.headerText == "BottleType")
                    {
                        nodeProperties.headerText += UnityEngine.Random.Range(1, 12);
                        LiquidLabyrinthBase.Logger.LogWarning("generating random name");
                    }
                    bottleType = nodeProperties.headerText;
                    nodeProperties.subText = $"Value: {itemProperties.creditsWorth}";
                }
                int NameHash = Math.Abs(nodeProperties.headerText.GetHashCode());
                float r = (NameHash * 16807) % 256 / 255f; // Pseudo-random number generator
                float g = ((NameHash * 48271) % 256) / 255f; // Pseudo-random number generator
                float b = ((NameHash * 69621) % 256) / 255f; // Pseudo-random number generator
                float lightenValue = 1f; // Adjust this value to control how much the color is lightened
                lighterColor.Value = new Color(Mathf.Clamp01(r + lightenValue),
                                              Mathf.Clamp01(g + lightenValue),
                                              Mathf.Clamp01(b + lightenValue));
                color.Value = new(r, g, b, 1);
            }
            if (Fill == -1)
            {
                Fill = UnityEngine.Random.Range(0f, 1f);
                LiquidLabyrinthBase.Logger.LogWarning("Bottle fill is -1, setting random value.");
            }
            if (Liquid != null)
            {
                rend = Liquid.GetComponent<MeshRenderer>();
                rend.material.SetColor("_LiquidColor", color.Value);
                rend.material.SetColor("_SurfaceColor", lighterColor.Value);
                rend.material.SetFloat("_Fill", Fill);
            }
        }

        // TODO: BETTER SAVES PLEASE I BEG YOU
        public override void LoadItemSaveData(int saveData)
        {
            base.LoadItemSaveData(saveData);
            LiquidLabyrinthBase.Logger.LogWarning($"LoadItemSaveData called! Got: {saveData}");
            Fill = (saveData / 100f);
            if (!NetworkManager.Singleton.IsHost) return;
            if (ES3.KeyExists("shipBottleData", GameNetworkManager.Instance.currentSaveFileName) && data == null)
            {
                data = ES3.Load<Dictionary<float, string>>("shipBottleData", GameNetworkManager.Instance.currentSaveFileName);
            }
            if (data == null) return;
            float key = saveData + Math.Abs(transform.position.x + transform.position.y + transform.position.z);
            if (data.TryGetValue(key, out string value))
            {
                GetComponentInChildren<ScanNodeProperties>().headerText = value;
                LiquidLabyrinthBase.Logger.LogWarning($"Found data: {value} ({key})");
            }
            else
            {
                LiquidLabyrinthBase.Logger.LogWarning($"Couldn't find save data for {GetType().Name}. ({key})");
            }
            LiquidLabyrinthBase.bottlesAdded++;
        }

        public override int GetItemDataToSave()
        {
            Dictionary<float, string> data = new Dictionary<float, string>();
            data.Add((int)(Fill * 100f) + Math.Abs(transform.position.x + transform.position.y + transform.position.z), GetComponentInChildren<ScanNodeProperties>().headerText);
            SaveUtils.AddToQueue<PotionBottle>(GetType(),data);
            return (int)(Fill * 100f);
        }

        public override void OnHitGround()
        {
            // Currently used for debugging
            if (Throwing)
            {
                LiquidLabyrinthBase.Logger.LogWarning("It hit da ground");
                //Landmine.SpawnExplosion(gameObject.transform.position, true, 1f, 4f);
            }
            // Run after so Throwing isn't set to false.
            base.OnHitGround();
        }


        // Debugging this
        public override void SetControlTipsForItem()
        {
            base.SetControlTipsForItem();
            string[] allLines;
            allLines = new string[]{
                $"BottleType: {bottleType}"
            };
            if (IsOwner)
            {
                HUDManager.Instance.ChangeControlTipMultiple(allLines, true, itemProperties);
            }
}

        public override void Update()
        {
            base.Update();
            if (Fill <= 0)
            {
                itemUsedUp = true;
                return;
            }
            MaxWobble = Fill * 0.2f;
            rend?.material.SetFloat("_Fill", Fill);
            Wobble();
        }


        // Should probably put this in throwable instead.
        public override void FallWithCurve()
        {
            base.FallWithCurve();
            float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, transform.eulerAngles.y, itemProperties.restingRotation.z), 14f * Time.deltaTime / magnitude);
            transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, FallCurve.Evaluate(fallTime));
            if (magnitude > 5f)
            {
                transform.localPosition = Vector3.Lerp(new Vector3(transform.localPosition.x, startFallingPosition.y, transform.localPosition.z), new Vector3(transform.localPosition.x, targetFloorPosition.y, transform.localPosition.z), VerticalFallCurveNoBounce.Evaluate(fallTime));
            }
            else
            {
                transform.localPosition = Vector3.Lerp(new Vector3(transform.localPosition.x, startFallingPosition.y, transform.localPosition.z), new Vector3(transform.localPosition.x, targetFloorPosition.y, transform.localPosition.z), VerticalFallCurve.Evaluate(fallTime));
            }
            fallTime += Mathf.Abs(Time.deltaTime * 12f / magnitude);
        }

        private void Wobble()
        {
            time += Time.deltaTime;
            // decrease wobble over time
            wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * (Recovery));
            wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * (Recovery));

            // make a sine wave of the decreasing wobble
            pulse = 2 * Mathf.PI * WobbleSpeed;
            wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
            wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

            // send it to the shader
            rend?.material.SetFloat("_WobbleX", wobbleAmountX);
            rend?.material.SetFloat("_WobbleZ", wobbleAmountZ);

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
    }
}
