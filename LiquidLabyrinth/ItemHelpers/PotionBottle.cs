using BepInEx;
using DunGen;
using GameNetcodeStuff;
using LiquidLabyrinth.Enums;
using LiquidLabyrinth.ItemData;
using LiquidLabyrinth.Labyrinth;
using LiquidLabyrinth.Labyrinth.Monobehaviours;
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
    private float _localFill = -1f;
    private string _localLiquidId = "";
    float maxEmission = 10f;

    [Space(3f)]
    [Header("Bottle Properties")]
    public Animator itemAnimator;

    public AudioClip openCorkSFX;

    public AudioClip closeCorkSFX;

    public AudioClip changeModeSFX;

    public AudioClip glassBreakSFX;
    public AudioClip liquidShakeSFX;

    [Space(3f)]
    [Header("Liquid Properties")]

    internal BottleContainerBehaviour containerbehaviour;

    public bool BreakBottle = false;
    public bool IsShaking = false;
    public int MaxHealth = 100;
    private NetworkVariable<int> Health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // This will be accessed by the server to determine whether or not to break the bottle. No need to network it. Maybe.
    private NetworkVariable<float> net_emission = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> net_CanRevivePlayer = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<FixedString128Bytes> net_Name = new NetworkVariable<FixedString128Bytes>("BottleType", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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

    void Awake()
    {
        containerbehaviour = GetComponentInChildren<BottleContainerBehaviour>();
    }

    public override void EquipItem()
    {
        base.EquipItem();
        playerHeldBy.equippedUsableItemQE = true;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
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
                net_Fill.Value = Random.Range(containerbehaviour.LowerLimit, containerbehaviour.UpperLimit);
                Plugin.Logger.LogWarning("Bottle fill is -1, setting random value.");
            }
            if (!_localLiquidId.IsNullOrWhiteSpace())
            {
                net_LiquidID.Value = _localLiquidId;
            }
            else
            {
                net_LiquidID.Value = LiquidAPI.RandomLiquid.ShortID;
            }
        }
        // ^ This part above only runs on server to sync initialization.
        nodeProperties.headerText = net_Name.Value.ToString();
        gameObject.GetComponent<MeshRenderer>().material.color = new Color(net_BottleColor.Value.g, net_BottleColor.Value.r, net_BottleColor.Value.b);
        if (containerbehaviour != null)
        {
            /*float range1 = Random.Range(containerbehaviour.LowerLimit, containerbehaviour.UpperLimit/2);
            float range2 = Random.Range(containerbehaviour.LowerLimit, range1);
            float range3 = Random.Range(containerbehaviour.LowerLimit, range2);*/
            containerbehaviour.AddLiquid(LiquidAPI.GetByID(net_LiquidID.Value.ToString()), net_Fill.Value);
            /*containerbehaviour.AddLiquid(LiquidAPI.RandomLiquid, range2);
            containerbehaviour.AddLiquid(LiquidAPI.RandomLiquid, range3); // its time for pain
            containerbehaviour.AddLiquid(LiquidAPI.RandomLiquid, Random.Range(containerbehaviour.LowerLimit, range3));*/
            if (containerbehaviour.LiquidContainer == null)
            {
                Plugin.Logger.LogWarning("Liquid container is null, i may scream at your console now.");
                return;
            }
            if (containerbehaviour.LiquidContainer.renderComponent == null)
            {
                Plugin.Logger.LogWarning("Render component is null, i may scream at your console now.");
                return;
            }
            rend = containerbehaviour.LiquidContainer.renderComponent;
            rend.material.SetFloat("_Emission", net_emission.Value);
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
        Plugin.Logger.LogWarning("load called");
        if (Data is BottleItemData itemData)
        {
            if (itemData == null) itemData = new("BottleType", 0f, LiquidAPI.RandomLiquid.ShortID);
            GetComponentInChildren<ScanNodeProperties>().headerText = itemData.name;
            _localFill = itemData.fill;

            if (!itemData.LiquidID.IsNullOrWhiteSpace())
            {
                _localLiquidId = itemData.LiquidID;
                Plugin.Logger.LogWarning($"GOT ID:{itemData.LiquidID}");
            }
            else
            {
                LiquidAPI.Liquid _Liquid = LiquidAPI.RandomLiquid;
                _localLiquidId = _Liquid.ShortID;
            }
        }
        else
        {
            Plugin.Logger.LogWarning($"Couldn't find save data for {GetType().Name} ({saveData}). Please send this log to the mod developer.");
        }
    }
        
    public override int GetItemDataToSave()
    {
        Data = new BottleItemData(net_Name.Value.ToString(), net_Fill.Value, net_LiquidID.Value.ToString());
        return base.GetItemDataToSave();
    }

    private static RaycastHit[] _potionBreakHits = new RaycastHit[20];

    [ServerRpc]
    void BreakBottle_ServerRpc()
    {
        Plugin.Logger.LogWarning("It hit da ground.");
        
        HitGround_ClientRpc(); // This is where stuff shatters, ect.
        DoPotionEffect();
        Destroy(gameObject, .5f);
    }

    void DoPotionEffect()
    {
        // Could be better to instantiate a liquid network object generated on runtime to handle these type of interactions.

        // It would also be better if I could somehow connect puddle creation and bottle breaking, as in spillage behaviour.
        // The effect chance percentage would depend on the puddle percentage from 0-100%. Not sure where i'll base the percentage on though.
        foreach (KeyValuePair<LiquidAPI.Liquid, Container.RefFill> liquid in containerbehaviour.LiquidDistribution)
        {
            containerbehaviour.OnContainerBreak(liquid.Key);
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

    bool broken = false;
    protected override void OnCollisionEnter(Collision collision)
    {
        LMBToThrow = false;
        if (rb.isKinematic || !StartOfRound.Instance.shipHasLanded)
        {
            base.OnCollisionEnter(collision);//run so it no fucky ok?
            return;
        }
        float impactForce = rb.velocity.magnitude;
        if(collision.rigidbody != null)
        {
            impactForce += collision.rigidbody.velocity.magnitude;
        }
        if (IsServer || IsHost) DamageBottle(impactForce);
        Plugin.Logger.LogWarning($"Health: {Health.Value}");
        if (!broken && BreakBottle && IsOwner)
        {
            broken = true;
            BreakBottle_ServerRpc();
        }
        // Call base method after doing logic, so isThrown isn't always set to false when checking.
        base.OnCollisionEnter(collision);
    }

    private void DamageBottle(float impactForce)
    {
        if (!(IsServer || IsHost))
        {
            Plugin.Logger.LogError("Client is trying to damage bottle, only the server should be allowed to do this!");
            return;
        }
        float damage = Mathf.Min(Mathf.Max(impactForce * (impactForce - Health.Value / 100), 0), Health.Value);
        Health.Value -= (int)damage;
        BreakBottle = Health.Value <= 0;
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
        light.color = containerbehaviour.LiquidContainer.Color;
        float distance = Vector3.Distance(lastNoisePosition, transform.position);
        Vector3 directionToNoise = (lastNoisePosition - transform.position).normalized;
        if (net_isFloating.Value)
        {
            rb.AddForce(directionToNoise * 0.005f, ForceMode.VelocityChange);
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
        //rend.material.SetFloat("_Fill", net_Fill.Value);
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
        if (IsOwner && playerHeldBy != null && net_mode.Value == BottleModes.Drink && Holding && net_isOpened.Value && playerHeldBy.playerBodyAnimator.GetCurrentAnimatorStateInfo(2).normalizedTime > 1)
        {
            containerbehaviour.Drain(Time.deltaTime);
            net_Fill.Value = containerbehaviour.TotalLiquidAmount;
            Plugin.Logger.LogWarning($"Started drinking! ({net_Fill.Value})");
        }
/*        if (net_Fill.Value <= 0.05) // fun
        {
            if (IsOwner)
            {
                net_Fill.Value = 0f;
                //rend.material.SetFloat("_Fill", 0f);
            }
            itemUsedUp = true;
            return;
        }*/
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