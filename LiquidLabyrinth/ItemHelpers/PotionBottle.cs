using BepInEx;
using DunGen;
using GameNetcodeStuff;
using LiquidLabyrinth.Enums;
using LiquidLabyrinth.ItemData;
using LiquidLabyrinth.Labyrinth;
using LiquidLabyrinth.Utilities;
using LiquidLabyrinth.Utilities.MonoBehaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace LiquidLabyrinth.ItemHelpers;
internal class PotionBottle : Throwable, INoiseListener
{
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
    private float _localFill = -1f;
    private string _localLiquidId = "";
    float wobbleAmountX;
    float wobbleAmountZ;
    float wobbleAmountToAddX;
    float wobbleAmountToAddZ;
    float maxEmission = 10f;
    float pulse;
    float time = 0.5f;

    [Space(3f)]
    [Header("Bottle Properties")]
    public Animator itemAnimator;

    public AudioClip openCorkSFX;

    public AudioClip closeCorkSFX;

    public AudioClip changeModeSFX;

    public AudioClip glassBreakSFX;
    public AudioClip liquidShakeSFX;

    public LiquidAPI.Liquid Liquid;
    [Space(3f)]
    [Header("Liquid Properties")]
    public bool BreakBottle = false;
    public bool IsShaking = false;
    private NetworkVariable<float> net_emission = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> net_CanRevivePlayer = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<FixedString128Bytes> net_Name = new NetworkVariable<FixedString128Bytes>("BottleType", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> net_playerHeldByInt = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> net_Fill = new NetworkVariable<float>(-1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> net_isOpened = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Color> net_BottleColor = new NetworkVariable<Color>(Color.red, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Color> net_LiquidColor = new NetworkVariable<Color>(Color.red, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<BottleModes> net_mode = new NetworkVariable<BottleModes>(BottleModes.Open, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<FixedString128Bytes> net_LiquidID = new NetworkVariable<FixedString128Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int _BottleModesLength = Enum.GetValues(typeof(BottleModes)).Length;
    private Light light;
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
        playerHeldBy.equippedUsableItemQE = true;
        wobbleAmountToAddX = wobbleAmountToAddX + Random.Range(1f, 10f);
        wobbleAmountToAddZ = wobbleAmountToAddZ + Random.Range(1f, 10f);
        if (IsOwner)
        {
            net_playerHeldByInt.Value = (int)playerHeldBy.playerClientId;
        }
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        if (IsOwner)
        {
            net_playerHeldByInt.Value = -1;
        }
    }

    public override void InteractItem()
    {
        base.InteractItem();
        Plugin.Logger.LogWarning("Interacted!");
    }


    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        LMBToThrow = false;
        Plugin.Logger.LogWarning($"{used} {buttonDown}");
        Holding = buttonDown;
        if (playerHeldBy == null) return;
        playerHeldBy.activatingItem = buttonDown;
        // Gotta make this a handler somehow.
        switch (net_mode.Value)
        {
            case BottleModes.Open:
                if (!buttonDown) break;
                // This is in reverse because we will set the value to this after the logic is done. (If a desync happens, the way this is set up with no RPCS, its gonna fucking explode planet earth. probably)
                itemAnimator.SetBool("CorkOpen", !net_isOpened.Value);
                if (!net_isOpened.Value)
                {
                    itemAudio.PlayOneShot(openCorkSFX);
                    RoundManager.Instance.PlayAudibleNoise(transform.position, 4f, 0.5f, 2, false, 0);
                }
                else
                {
                    itemAudio.PlayOneShot(closeCorkSFX);
                    RoundManager.Instance.PlayAudibleNoise(transform.position, 4f, 0.5f, 1, false, 0);
                    
                }
                // Do this last to not cause any network issues lmao.
                if (IsOwner)
                {
                    net_isOpened.Value = !net_isOpened.Value;
                    SetControlTipsForItem();
                }
                break;
            case BottleModes.Drink:
                if (!net_isOpened.Value) break;
                playerHeldBy.playerBodyAnimator.SetBool("useTZPItem", buttonDown);
                break;
            case BottleModes.Throw:
                LMBToThrow = true;
                playerThrownBy = playerHeldBy;
                if (IsOwner && Random.Range(1, 100) <= 25) BreakBottle = true;
                break;
            case BottleModes.Toast:
                if (!buttonDown || !IsOwner) break;
                if (ToastCoroutine != null) break;
                ToastCoroutine = StartCoroutine(Toast());
                break;
            case BottleModes.Shake:
                IsShaking = buttonDown;
                if (!buttonDown) break;
                CoroutineHandler.Instance.NewCoroutine<PotionBottle>(ShakeBottle());
                break;
        }
        base.ItemActivate(used, buttonDown);
    }

    private IEnumerator ShakeBottle()
    {
        // bad code
        playerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
        AnimatorStateInfo stateInfo = playerHeldBy.playerBodyAnimator.GetCurrentAnimatorStateInfo(2);
        float animationDuration = stateInfo.length;
        float _start = Random.Range(0, liquidShakeSFX.length - animationDuration);
        float _stop = Mathf.Min(_start + animationDuration, liquidShakeSFX.length);
        AudioClip subclip = liquidShakeSFX.MakeSubclip(_start, _stop);
        itemAudio.PlayOneShot(subclip);
        RoundManager.Instance.PlayAudibleNoise(transform.position, 4f, 0.5f, 2, false, 0);
        CoroutineHandler.Instance.NewCoroutine<PotionBottle>(itemAudio.FadeOut(1f));
        wobbleAmountToAddX = wobbleAmountToAddX + Random.Range(1f, 10f);
        wobbleAmountToAddZ = wobbleAmountToAddZ + Random.Range(1f, 10f);
        while (Holding)
        {
            yield return new WaitForSeconds(0.05f);
            stateInfo = playerHeldBy.playerBodyAnimator.GetCurrentAnimatorStateInfo(2);
            if (stateInfo.IsName("ShakeItem") && stateInfo.normalizedTime > 0.76f)
            {
                playerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
                animationDuration = stateInfo.length;
                _start = Random.Range(0, liquidShakeSFX.length - animationDuration);
                _stop = Mathf.Min(_start + animationDuration, liquidShakeSFX.length);
                subclip = liquidShakeSFX.MakeSubclip(_start, _stop);
                itemAudio.PlayOneShot(subclip);
                RoundManager.Instance.PlayAudibleNoise(transform.position, 4f, 0.5f, 2, false, 0);
                CoroutineHandler.Instance.NewCoroutine<PotionBottle>(itemAudio.FadeOut(1f));
                wobbleAmountToAddX = wobbleAmountToAddX + Random.Range(1f, 10f);
                wobbleAmountToAddZ = wobbleAmountToAddZ + Random.Range(1f, 10f);
            }
        }
        yield break;
    }

    // this shit don't work.
    private Coroutine? ToastCoroutine;
    private IEnumerator Toast()
    {
        RaycastHit[] hits = Physics.SphereCastAll(new Ray(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward * 20f, playerHeldBy.gameplayCamera.transform.forward), 20f, 80f, LayerMask.GetMask("Props"));
        if (hits.Count() > 0)
        {
            RaycastHit found = hits.FirstOrDefault(hit => hit.transform.TryGetComponent(out PotionBottle bottle) && bottle.isHeld && bottle.playerHeldBy != GameNetworkManager.Instance.localPlayerController);// && bottle.isHeld && bottle.playerHeldBy != GameNetworkManager.Instance.localPlayerController
            Plugin.Logger.LogWarning(found.transform);
            if (found.transform != null && playerHeldBy != null)
            {
                Plugin.Logger.LogWarning("FOUND!");
                if (IsOwner) playerHeldBy.UpdateSpecialAnimationValue(true, 0, playerHeldBy.targetYRot, true);
                PlayerUtils.RotateToObject(playerHeldBy, found.transform.gameObject);
                playerHeldBy.inSpecialInteractAnimation = true;
                playerHeldBy.isClimbingLadder = false;
                playerHeldBy.playerBodyAnimator.ResetTrigger("SA_ChargeItem");
                playerHeldBy.playerBodyAnimator.SetTrigger("SA_ChargeItem");
                yield return new WaitForSeconds(0.5f);
                playerHeldBy.playerBodyAnimator.ResetTrigger("SA_ChargeItem");
                if (IsOwner) playerHeldBy.UpdateSpecialAnimationValue(false, 0, 0f, false);
                playerHeldBy.activatingItem = false;
                playerHeldBy.inSpecialInteractAnimation = false;
                playerHeldBy.isClimbingLadder = false;
                Plugin.Logger.LogWarning("DONE ANIMATION");
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
            net_mode.Value -= 1;
            if ((int)net_mode.Value == -1) net_mode.Value = (BottleModes)(_BottleModesLength - 1);
        }
        else
        {
            net_mode.Value += 1;
            if ((int)net_mode.Value == _BottleModesLength) net_mode.Value = 0;
        }
        itemAudio.PlayOneShot(changeModeSFX);
        SetControlTipsForItem();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        light = GetComponentInChildren<Light>();
        floatWhileOrbiting = true;
        ScanNodeProperties nodeProperties = GetComponentInChildren<ScanNodeProperties>();
        if (IsHost || IsServer)
        {
            scrapValue = itemProperties.creditsWorth;
            net_CanRevivePlayer.Value = Plugin.Instance.RevivePlayer.Value;
            if (nodeProperties != null)
            {
                if (nodeProperties.headerText == "BottleType")
                {
                    nodeProperties.headerText = MarkovChain.GenerateText(Random.Range(3,14), 128);
                    Plugin.Logger.LogWarning("generating random name");
                }
                net_Name.Value = new FixedString128Bytes(nodeProperties.headerText);
            }
            int NameHash = Math.Abs(nodeProperties.headerText.GetHashCode());
            float r = (NameHash * 16807) % 256 / 255f; // Pseudo-random number generator
            float g = ((NameHash * 48271) % 256) / 255f; // Pseudo-random number generator
            float b = ((NameHash * 69621) % 256) / 255f; // Pseudo-random number generator
            net_BottleColor.Value = new(r, g, b, 1);
            if (_localFill != -1)
            {
                net_Fill.Value = _localFill;
            }
            if (net_Fill.Value == -1)
            {
                net_Fill.Value = Random.Range(0f, 1f);
                Plugin.Logger.LogWarning("Bottle fill is -1, setting random value.");
            }
            if (net_LiquidID.Value.ToString().IsNullOrWhiteSpace()) 
            {
                if (_localLiquidId.IsNullOrWhiteSpace())
                {
                    Liquid = LiquidAPI.RandomLiquid;
                    net_LiquidID.Value = Liquid.ShortID;
                }
                else
                {
                    net_LiquidID.Value = _localLiquidId;
                    Liquid = LiquidAPI.GetByID(net_LiquidID.Value.ToString());
                    if(Liquid == null)
                    {
                        Plugin.Logger.LogWarning("Liquid ID search returned null; Generating random liquid (server)");
                        Liquid = LiquidAPI.RandomLiquid;
                        net_LiquidID.Value = Liquid.ShortID;
                    }
                }
            }
            if(Liquid != null)
            {
                net_LiquidColor.Value = Liquid.Color;
            }
        }
        // ^ This part above only runs on server to sync initialization.
        if (net_playerHeldByInt.Value != -1)
        {
            playerHeldBy = StartOfRound.Instance.allPlayerScripts[net_playerHeldByInt.Value];
        }
        Liquid = LiquidAPI.GetByID(net_LiquidID.Value.ToString());
        // Sync to all clients.
        nodeProperties.headerText = net_Name.Value.ToString();
        gameObject.GetComponent<MeshRenderer>().material.color = new Color(net_BottleColor.Value.g, net_BottleColor.Value.r, net_BottleColor.Value.b);
        if (Liquid != null)
        {
            Plugin.Logger.LogWarning($"GOT: {Liquid.Name} from {Liquid.ModName}");
            Liquid.Container = transform.Find("Liquid").gameObject;
            rend = Liquid.Container.GetComponent<MeshRenderer>();
            nodeProperties.subText += $"\nLiquid(s): {Liquid.Name}";
            rend.material.SetFloat("_Emission", net_emission.Value);
            rend.material.SetFloat("_Fill", net_Fill.Value);
            if (net_LiquidColor.Value == null) return;
            Color _lighterColor = new Color(Mathf.Clamp01(net_LiquidColor.Value.r + 1f),
                Mathf.Clamp01(net_LiquidColor.Value.g + 1f),
                Mathf.Clamp01(net_LiquidColor.Value.b + 1f));
            rend.material.SetColor("_SurfaceColor", _lighterColor);
            rend.material.SetColor("_LiquidColor", net_LiquidColor.Value);
            light.color = net_LiquidColor.Value;
        }
        else
        {
            Plugin.Logger.LogError("Liquid is null on client; This shouldn't happen!");
        }
        itemAnimator.SetBool("CorkOpen", net_isOpened.Value);
    }

    public override void LoadItemSaveData(int saveData)
    {
        DataType = typeof(BottleItemData);
        base.LoadItemSaveData(saveData);
        if (!NetworkManager.Singleton.IsHost || !NetworkManager.Singleton.IsServer) return; // Return if not host or server. (bottom part prob already wont run if youre client, but just incase)
        if (Data is BottleItemData itemData)
        {
            if (itemData == null) itemData = new("BottleType", 0f, Liquid.ShortID);
            GetComponentInChildren<ScanNodeProperties>().headerText = itemData.name;
            _localFill = (itemData.fill);
            _localLiquidId = itemData.LiquidID;
        }
        else
        {
            Plugin.Logger.LogWarning($"Couldn't find save data for {GetType().Name} ({saveData}). Please send this log to the mod developer.");
        }
    }
        
    public override int GetItemDataToSave()
    {
        Data = new BottleItemData(net_Name.Value.ToString(), net_Fill.Value, Liquid.ShortID);
        return base.GetItemDataToSave();
    }

    private static RaycastHit[] _potionBreakHits = new RaycastHit[20];

    [ServerRpc]
    void HitGround_ServerRpc()
    {
        Plugin.Logger.LogWarning("It hit da ground");
        
        HitGround_ClientRpc();
        DoPotionEffect();
        Destroy(gameObject, .5f);
    }

    void DoPotionEffect()
    {
        // REVIVE TEST:
        var size = Physics.SphereCastNonAlloc(new Ray(gameObject.transform.position + gameObject.transform.up * 2f, gameObject.transform.forward), 3f, _potionBreakHits, 2f, 1572872);
        if (size == 0)
        {
            Liquid.OnContainerBreak();
        }

        foreach (RaycastHit hit in _potionBreakHits)
        {
            if (hit.transform == null) return;
            Liquid.OnContainerBreak(hit);
        }
    }
    
    [ClientRpc]
    void HitGround_ClientRpc()
    {
        // TODO: Glass shatter effect and puddle instantiation
        EnableItemMeshes(false);
        var audioSource = gameObject.GetComponent<AudioSource>();
        RoundManager.Instance.PlayAudibleNoise(transform.position, 10f, 1, 0, false, 0);
        audioSource.PlayOneShot(glassBreakSFX, 1f);
        audioSource.PlayOneShot(itemProperties.dropSFX, 1f);
    }

    public override void OnCollisionEnter(Collision collision)
    {
        LMBToThrow = false;
        if (rb.isKinematic) return;
        if (isThrown.Value && BreakBottle)
        {
            HitGround_ServerRpc();
        }
        // Call base function after doing logic, so isThrown isn't always set to false when checking.
        base.OnCollisionEnter(collision);
    }


    // Debugging this
    public override void SetControlTipsForItem()
    {
        base.SetControlTipsForItem();
        string[] allLines;
        string modeString = GetModeString(net_mode.Value, net_isOpened.Value);
        allLines = [
            $"Name: Bottle of {net_Name.Value}",
            $"Mode: {modeString}",
            "Switch Mode [Q/E]"
        ];
        if (IsOwner)
        {
            HUDManager.Instance.ChangeControlTipMultiple(allLines, true, itemProperties);
        }
    }
    float elapsedTime = 0.0f; // time elapsed since the decay started
    float shakeTime = 0.0f; // time elapsed since the decay started
    public override void Update()
    {
        base.Update();
        float distance = Vector3.Distance(lastNoisePosition, transform.position);
        Vector3 directionToNoise = (new Vector3(0, lastNoisePosition.y, 0) - new Vector3(0, transform.position.y, 0)).normalized;
        if (net_isFloating.Value)
        {
            rb.AddForce(directionToNoise * 0.05f, ForceMode.VelocityChange);
            Quaternion targetRotation = Random.rotation;

            // Calculate the difference between the current rotation and the target rotation
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(rb.rotation);

            // Convert the rotation difference to a torque
            Vector3 torque = new Vector3(deltaRotation.x, deltaRotation.y, deltaRotation.z) * 10f; // adjust the multiplier as needed

            // Apply the torque
            rb.AddTorque(torque, ForceMode.Acceleration);
        }
        if (distance > 4.2f && (IsServer || IsHost))
        {
            net_isFloating.Value = false;
        }
        float lerpFactor = 0.2f * Time.deltaTime;
        rend.material.SetFloat("_Emission", net_emission.Value);
        rend.material.SetFloat("_Fill", net_Fill.Value);
        light.intensity = net_emission.Value;
        if (itemUsedUp) return;
        if ((IsShaking || net_isFloating.Value) && IsOwner)
        {
            net_emission.Value = Mathf.Lerp(net_emission.Value, maxEmission, lerpFactor);
            shakeTime += Time.deltaTime; // increment the elapsed time
        }
        else if (net_emission.Value > 0.001f && IsOwner)
        {
            float t = elapsedTime / shakeTime; // calculate the interpolation factor
            net_emission.Value = Mathf.Lerp(net_emission.Value, 0, t); // interpolate between start and end values
            elapsedTime += Time.deltaTime; // increment the elapsed time
        }
        else if(IsOwner)
        {
            net_emission.Value = 0f;
            elapsedTime = 0f;
            elapsedTime = 0f;
        } // i suck at maths btw.
        MaxWobble = net_Fill.Value * 0.2f;
        if (IsOwner && playerHeldBy != null && net_mode.Value == BottleModes.Drink && Holding && net_isOpened.Value && playerHeldBy.playerBodyAnimator.GetCurrentAnimatorStateInfo(2).normalizedTime > 1)
        {
            net_Fill.Value = Mathf.Lerp(net_Fill.Value, 0f, Time.deltaTime * 0.5f);
            Plugin.Logger.LogWarning($"Started drinking! ({net_Fill.Value})");
        }
        if (net_Fill.Value <= 0.05) // fun
        {
            if (IsOwner)
            {
                net_Fill.Value = 0f;
                rend.material.SetFloat("_Fill", 0f);
            }
            itemUsedUp = true;
            return;
        }
        Wobble();
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

    // lmao.
    Vector3 lastNoisePosition;
    public void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot, int noiseID)
    {
        if (!(IsHost || IsServer)) return;
        if (noiseID != 5) return;
        lastNoisePosition = noisePosition;
        net_isFloating.Value = true;
    }
}