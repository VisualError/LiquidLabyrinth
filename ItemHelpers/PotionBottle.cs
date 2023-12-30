using GameNetcodeStuff;
using LiquidLabyrinth.Enums;
using LiquidLabyrinth.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LiquidLabyrinth.ItemHelpers
{
    class PotionBottle : Throwable
    {
        private Dictionary<float, string> data = null;
        private Renderer rend;
        [Header("Wobble Settings")]
        Vector3 lastPos;
        Vector3 velocity;
        Vector3 lastRot;
        Vector3 angularVelocity;
        public float MaxWobble = 1f;
        //private float MaxWobbleBase = 1f;
        public float WobbleSpeed = 1f;
        public float Recovery = 1f;
        private float localFill = -1f;
        float wobbleAmountX;
        float wobbleAmountZ;
        float wobbleAmountToAddX;
        float wobbleAmountToAddZ;
        float pulse;
        float time = 0.5f;

        [Space(3f)]
        [Header("Bottle Properties")]
        public Animator itemAnimator;

        public AudioSource itemAudio;

        public AudioClip openCorkSFX;

        public AudioClip closeCorkSFX;

        public AudioClip changeModeSFX;

        public AudioClip glassBreakSFX;

        public GameObject Liquid;
        public List<string> BottleProperties; // TODO: Add bottle properties class. API in mind, change `string` into the class.
        string bottleType; // for debugging only
        [Space(3f)]
        [Header("Liquid Properties")]
        public bool BreakBottle = false;
        private NetworkVariable<int> playerHeldByInt = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> Fill = new NetworkVariable<float>(-1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<bool> isOpened = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<Color> color = new NetworkVariable<Color>(Color.red, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Color> lighterColor = new NetworkVariable<Color>(Color.blue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        [SerializeField] private NetworkVariable<BottleModes> mode = new NetworkVariable<BottleModes>(BottleModes.Open, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private int BottleModesLength = Enum.GetValues(typeof(BottleModes)).Length;
        public string GetModeString(BottleModes mode, bool isOpened)
        {
            if (mode == BottleModes.Open && isOpened)
            {
                return "Close";
            }
            else
            {
                return Enum.GetName(typeof(BottleModes), mode);
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            EnablePhysics(true);
            playerHeldBy.equippedUsableItemQE = true;
            wobbleAmountToAddX = wobbleAmountToAddX + UnityEngine.Random.Range(1f, 10f);
            wobbleAmountToAddZ = wobbleAmountToAddZ + UnityEngine.Random.Range(1f, 10f);
            if (IsOwner)
            {
                playerHeldByInt.Value = (int)playerHeldBy.playerClientId;
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            if (IsOwner)
            {
                playerHeldByInt.Value = -1;
            }
        }

        public override void InteractItem()
        {
            base.InteractItem();
            EnablePhysics(true);
            LiquidLabyrinthBase.Logger.LogWarning("Interacted!");
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            LMBToThrow = false;
            LiquidLabyrinthBase.Logger.LogWarning($"{used} {buttonDown}");
            Holding = buttonDown;
            if (playerHeldBy == null) return;
            playerHeldBy.activatingItem = buttonDown;
            switch (mode.Value)
            {
                case BottleModes.Open:
                    if (!buttonDown) break;
                    // This is in reverse because we will set the value to this after the logic is done. (If a desync happens, the way this is set up with no RPCS, its gonna fucking explode planet earth)
                    itemAnimator.SetBool("CorkOpen", !isOpened.Value);
                    if (!isOpened.Value)
                    {
                        itemAudio.PlayOneShot(openCorkSFX);
                    }
                    else
                    {
                        itemAudio.PlayOneShot(closeCorkSFX);
                    }
                    // Do this last to not cause any network issues lmao.
                    if (IsOwner)
                    {
                        isOpened.Value = !isOpened.Value;
                        SetControlTipsForItem();
                    }
                    break;
                case BottleModes.Drink:
                    if (!isOpened.Value) break;
                    playerHeldBy.playerBodyAnimator.SetBool("useTZPItem", buttonDown);
                    //itemAnimator.SetBool("Drink", buttonDown);
                    break;
                case BottleModes.Throw:
                    LMBToThrow = true;
                    if (IsOwner && UnityEngine.Random.Range(1, 100) <= 25) BreakBottle = true;
                    break;
                case BottleModes.Toast:
                    if (playerHeldBy == null || !buttonDown || !IsOwner) break;
                    if (ToastCoroutine != null) break;
                    ToastCoroutine = StartCoroutine(Toast());
                    break;
            }
            base.ItemActivate(used, buttonDown);
        }
        // this shit don't work.
        private Coroutine ToastCoroutine;
        private IEnumerator Toast()
        {
            RaycastHit[] hits = Physics.SphereCastAll(new Ray(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward * 20f, playerHeldBy.gameplayCamera.transform.forward), 20f, 80f, LayerMask.GetMask("Props"));
            if (hits.Count() > 0)
            {
                RaycastHit found = hits.FirstOrDefault(hit => hit.transform.TryGetComponent(out PotionBottle bottle) && bottle.playerHeldBy != GameNetworkManager.Instance.localPlayerController);// && bottle.isHeld && bottle.playerHeldBy != GameNetworkManager.Instance.localPlayerController
                LiquidLabyrinthBase.Logger.LogWarning(found.transform);
                if (found.transform != null && playerHeldBy != null)
                {
                    LiquidLabyrinthBase.Logger.LogWarning("FOUND!");
                    EnablePhysics(false);
                    if (IsOwner) playerHeldBy.UpdateSpecialAnimationValue(true, 0, playerHeldBy.targetYRot, true);
                    PlayerUtils.RotateToObject(playerHeldBy, found.transform.gameObject);
                    playerHeldBy.inSpecialInteractAnimation = true;
                    playerHeldBy.isClimbingLadder = false;
                    playerHeldBy.playerBodyAnimator.ResetTrigger("SA_ChargeItem");
                    playerHeldBy.playerBodyAnimator.SetTrigger("SA_ChargeItem");
                    yield return new WaitForSeconds(0.5f);
                    EnablePhysics(true);
                    playerHeldBy.playerBodyAnimator.ResetTrigger("SA_ChargeItem");
                    if (IsOwner) playerHeldBy.UpdateSpecialAnimationValue(false, 0, 0f, false);
                    playerHeldBy.activatingItem = false;
                    playerHeldBy.inSpecialInteractAnimation = false;
                    playerHeldBy.isClimbingLadder = false;
                    LiquidLabyrinthBase.Logger.LogWarning("DONE ANIMATION");
                }
            }
            ToastCoroutine = null;
            yield break;
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (playerHeldBy != null && playerHeldBy.activatingItem) return;
            if (!IsOwner) return;
            if (right)
            {
                mode.Value -= 1;
                if ((int)mode.Value == -1) mode.Value = (BottleModes)(BottleModesLength - 1);
            }
            else
            {
                mode.Value += 1;
                if ((int)mode.Value == BottleModesLength) mode.Value = 0;
            }
            itemAudio.PlayOneShot(changeModeSFX);
            SetControlTipsForItem();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsHost || IsServer)
            {
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
                if (localFill != -1)
                {
                    Fill.Value = localFill;
                }
                if (Fill.Value == -1)
                {
                    Fill.Value = UnityEngine.Random.Range(0f, 1f);
                    LiquidLabyrinthBase.Logger.LogWarning("Bottle fill is -1, setting random value.");
                }
            }
            // ^ This part above only runs on server to sync initialization.
            if (playerHeldByInt.Value != -1)
            {
                playerHeldBy = StartOfRound.Instance.allPlayerScripts[playerHeldByInt.Value];
            }
            // Sync to all clients.
            gameObject.GetComponent<MeshRenderer>().material.color = new Color(color.Value.g, color.Value.r, color.Value.b);
            if (Liquid != null)
            {
                rend = Liquid.GetComponent<MeshRenderer>();
                rend.material.SetColor("_LiquidColor", color.Value);
                rend.material.SetColor("_SurfaceColor", lighterColor.Value);
                rend.material.SetFloat("_Fill", Fill.Value);
            }
            itemAnimator.SetBool("CorkOpen", isOpened.Value);
        }

        public override void LoadItemSaveData(int saveData)
        {
            base.LoadItemSaveData(saveData);
            LiquidLabyrinthBase.Logger.LogWarning($"LoadItemSaveData called! Got: {saveData}");
            localFill = (saveData / 100f);
            if (!NetworkManager.Singleton.IsHost || !NetworkManager.Singleton.IsServer) return; // Return if not host or server.
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
                LiquidLabyrinthBase.Logger.LogWarning($"Couldn't find save data for {GetType().Name}. ({key}). Please send this log to the mod developer.");
            }
        }



        public override int GetItemDataToSave()
        {
            Dictionary<float, string> data = new Dictionary<float, string>();
            data.Add((int)(Fill.Value * 100f) + Math.Abs(transform.position.x + transform.position.y + transform.position.z), GetComponentInChildren<ScanNodeProperties>().headerText);
            SaveUtils.AddToQueue<PotionBottle>(GetType(), data);
            return (int)(Fill.Value * 100f);
        }


        [ServerRpc(RequireOwnership = false)]
        void HitGround_ServerRpc()
        {
            HitGround_ClientRpc();
        }

        [ClientRpc]
        void HitGround_ClientRpc()
        {
            LiquidLabyrinthBase.Logger.LogWarning("It hit da ground");


            // REVIVE TEST:
            RaycastHit[] hits = Physics.SphereCastAll(new Ray(gameObject.transform.position + gameObject.transform.up * 2f, gameObject.transform.forward), 10f, 80f, 1048576);

            //END OF REVIVE TEST
            if (hits.Count() > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform.TryGetComponent(out DeadBodyInfo deadBodyInfo))
                    {
                        PlayerControllerB player = deadBodyInfo.playerScript;
                        PlayerUtils.RevivePlayer(player, deadBodyInfo, hit.transform.position);
                    }
                }
            }


            // TODO: Glass shatter effect and puddle instansiation
            AudioSource.PlayClipAtPoint(glassBreakSFX, gameObject.transform.position);
            AudioSource.PlayClipAtPoint(itemProperties.dropSFX, gameObject.transform.position);
            if (IsServer || IsHost) Destroy(gameObject);
        }

        public override void OnHitGround()
        {
            // Currently used for debugging
            LMBToThrow = false;
            if (Throwing.Value && BreakBottle)
            {
                HitGround_ServerRpc();
            }
            // Run after so Throwing isn't set to false.
            base.OnHitGround();
        }


        // Debugging this
        public override void SetControlTipsForItem()
        {
            base.SetControlTipsForItem();
            string[] allLines;
            string modeString = GetModeString(mode.Value, isOpened.Value);
            allLines = new string[]{
                $"BottleType: {bottleType}",
                $"Mode: {modeString}"
            };
            if (IsOwner)
            {
                HUDManager.Instance.ChangeControlTipMultiple(allLines, true, itemProperties);
            }
        }

        public override void Update()
        {
            base.Update();
            if (itemUsedUp) return;


            MaxWobble = Fill.Value * 0.2f;
            if (IsOwner && playerHeldBy != null && mode.Value == BottleModes.Drink && Holding && isOpened.Value && playerHeldBy.playerBodyAnimator.GetCurrentAnimatorStateInfo(2).normalizedTime > 1)
            {
                Fill.Value = Mathf.Lerp(Fill.Value, 0f, Time.deltaTime * 0.5f);
                LiquidLabyrinthBase.Logger.LogWarning($"Started drinking! ({Fill.Value})");
            }
            if (Fill.Value <= 0.05) // fun
            {
                if (IsOwner)
                {
                    Fill.Value = 0f;
                    rend.material.SetFloat("_Fill", 0f);
                }
                itemUsedUp = true;
                return;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            ContactPoint contact = collision.contacts[0];
            Debug.Log($"CONTACT!");
        }

            public void FixedUpdate()
        {
            if (itemUsedUp) return;
            rend.material.SetFloat("_Fill", Fill.Value);
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
            rend.material.SetFloat("_WobbleX", wobbleAmountX);
            rend.material.SetFloat("_WobbleZ", wobbleAmountZ);

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
