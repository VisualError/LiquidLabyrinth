using GameNetcodeStuff;
using LiquidLabyrinth.Utilities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using BepInEx;

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
    private Dictionary<float, string> data = null;
    private string _localtooltip;
    private string _localdescription;
    bool Equiped = false;
    private NetworkVariable<FixedString32Bytes> net_tooltip = new NetworkVariable<FixedString32Bytes>("Head.", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<FixedString32Bytes> net_description = new NetworkVariable<FixedString32Bytes>("Unknown.", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        var player = GetComponentInParent<DeadBodyInfo>(true);
        var scanNode = GetComponentInChildren<ScanNodeProperties>();
        if(IsServer || IsHost)
        {
            if (!_localtooltip.IsNullOrWhiteSpace() && !_localdescription.IsNullOrWhiteSpace())
            {
                net_tooltip.Value = $"{_localtooltip}";
                net_description.Value = _localdescription;
            }
            else if(player != null && scanNode != null)
            {
                net_tooltip.Value = $"{player.playerScript.playerUsername}'s Head.";
                net_description.Value = Enum.GetName(typeof(CauseOfDeath), player.causeOfDeath);
                customGrabTooltip = $"{net_tooltip.Value} [E]";
                scanNode.headerText = $"{net_tooltip.Value}";
                scanNode.subText = $"{net_description.Value}";
            }
        }
    }

    public override int GetItemDataToSave()
    {
        Plugin.Instance.headItemList.Add(this);

        Dictionary<float, string> data = new Dictionary<float, string>();
        HeadItemData obj = new (net_tooltip.Value.ToString(), net_description.Value.ToString());
        data.Add(Plugin.Instance.headItemList.Count, JsonUtility.ToJson(obj));
        SaveUtils.AddToQueue<HeadItem>(GetType(), data, "shipHeadData");
        return Plugin.Instance.headItemList.Count;
    }

    public override void LoadItemSaveData(int saveData)
    {
        base.LoadItemSaveData(saveData);
        if (!NetworkManager.Singleton.IsHost || !NetworkManager.Singleton.IsServer) return; // Return if not host or server.
        if (ES3.KeyExists("shipHeadData", GameNetworkManager.Instance.currentSaveFileName) && data == null)
        {
            data = ES3.Load<Dictionary<float, string>>("shipHeadData", GameNetworkManager.Instance.currentSaveFileName);
        }
        if (data == null) return;
        float key = saveData;
        if (data.TryGetValue(key, out string value))
        {
            var scanNode = GetComponentInChildren<ScanNodeProperties>();
            var dataObject = JsonUtility.FromJson<HeadItemData>(value);
            if (dataObject == null) return;
            _localtooltip = dataObject.tooltip;
            _localdescription = dataObject.description;
            customGrabTooltip = $"{dataObject.tooltip} [E]";
            scanNode.headerText = $"{dataObject.tooltip}";
            scanNode.subText = $"{dataObject.description}";
            Plugin.Logger.Log(BepInEx.Logging.LogLevel.All,$"Found data: {value} ({key})");
        }
        else
        {
            Plugin.Logger.Log(BepInEx.Logging.LogLevel.All,$"Couldn't find save data for {GetType().Name}. ({key}). Please send this log to the mod developer.");
        }
    }

    public override void Update()
    {
        base.Update();
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