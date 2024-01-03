using GameNetcodeStuff;
using LiquidLabyrinth.Utilities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using BepInEx;
using DunGen;
using static UnityEngine.UIElements.StylePropertyAnimationSystem;
using System.Runtime.Serialization;

namespace LiquidLabyrinth.ItemHelpers;

[Serializable]
public class HeadItemData
{
    public HeadItemData(string tip, string desc)
    {
        tooltip = tip;
        description = desc;
    }
    public bool IsNullOrEmpty()
    {
        return string.IsNullOrEmpty(tooltip) && string.IsNullOrEmpty(description);
    }
    public string tooltip;
    public string description;
}
class HeadItem : Throwable
{

    private string _localtooltip;
    private string _localdescription;
    bool Equiped = false;
    private NetworkVariable<FixedString32Bytes> net_tooltip = new NetworkVariable<FixedString32Bytes>("Head.", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<FixedString32Bytes> net_description = new NetworkVariable<FixedString32Bytes>("Unknown.", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Plugin.Logger.LogWarning("network called");
        var player = GetComponentInParent<DeadBodyInfo>(true);
        var scanNode = GetComponentInChildren<ScanNodeProperties>();
        if(IsServer || IsHost)
        {
            if (player != null && scanNode != null)
            {
                Plugin.Logger.LogWarning("found player");
                net_tooltip.Value = $"{player.playerScript.playerUsername}'s Head.";
                net_description.Value = Enum.GetName(typeof(CauseOfDeath), player.causeOfDeath);
            }
            else
            {
                Plugin.Logger.LogWarning("Couldn't find Player");
                if (_localtooltip.IsNullOrWhiteSpace() && _localdescription.IsNullOrWhiteSpace()) return;
                net_tooltip.Value = $"{_localtooltip}";
                net_description.Value = _localdescription;
            }
        }
        customGrabTooltip = $"{net_tooltip.Value} [E]";
        if (scanNode == null) return;
        scanNode.headerText = $"{net_tooltip.Value}";
        scanNode.subText = $"{net_description.Value}";
    }

    public override int GetItemDataToSave()
    {
        Data = new HeadItemData(net_tooltip.Value.ToString(), net_description.Value.ToString());
        return base.GetItemDataToSave();
    }

    public override void LoadItemSaveData(int saveData)
    {
        DataType = typeof(HeadItemData);
        base.LoadItemSaveData(saveData);
        if (!NetworkManager.Singleton.IsHost || !NetworkManager.Singleton.IsServer) return; // Return if not host or server. (bottom part prob already wont run if youre client, but just incase)
        if (Data is HeadItemData itemData && itemData != null)
        {
            _localtooltip = itemData.tooltip;
            _localdescription = itemData.description;
        }
        // Using local variables because network object hasn't been spawned on the server yet. These values will be useful for OnNetworkSpawn
    }

    public override void Update()
    {
        base.Update();
        if (!IsSpawned) return;
        PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
        if (localPlayerController == null) return;
        float divided = localPlayerController.insanityLevel + 1f / localPlayerController.maxInsanityLevel + 1f;
        float num = Vector3.Distance(localPlayerController.gameplayCamera.transform.position, transform.position);
        if(num > 10f || playerHeldBy == localPlayerController && !Equiped)
        {
            localPlayerController.isMovementHindered = (int)Mathf.MoveTowards(localPlayerController.isMovementHindered, 0f, Time.deltaTime);
        }
        if (localPlayerController.HasLineOfSightToPosition(transform.position, 30f / (num / 5f), 60, -1f))
        {
            if (playerHeldBy == localPlayerController && !Equiped) return;
            if (num < 10f)
            {
                localPlayerController.insanityLevel += 0.75f / (divided);
                localPlayerController.insanitySpeedMultiplier += 0.75f / (divided);
                localPlayerController.isMovementHindered = (int)(divided - 1);
            }
            else
            {
                localPlayerController.insanityLevel += 0.3f / (divided);
                localPlayerController.insanitySpeedMultiplier += 0.3f / (divided);
                return;
            }
            localPlayerController.JumpToFearLevel(localPlayerController.insanityLevel/localPlayerController.maxInsanityLevel, true);
            if (playerHeldBy != null)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
            }
        }
    }

    public override void EquipItem()
    {
        base.EquipItem();
        Equiped = true;
    }
    public override void DiscardItem()
    {
        base.DiscardItem();
        Equiped = false;
    }
    public override void PocketItem()
    {
        base.PocketItem();
        Equiped = false;
    }

}