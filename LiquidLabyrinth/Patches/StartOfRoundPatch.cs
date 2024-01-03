using BepInEx;
using HarmonyLib;
using LiquidLabyrinth.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace LiquidLabyrinth.Patches;

[HarmonyPatch(typeof(StartOfRound))]
class StartOfRoundPatch
{
    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPostfix]
    static void AwakePostfix()
    {
        Plugin.Logger.LogWarning("awake");
        //Enemy dictionary init.
        foreach (EnemyType type in Resources.FindObjectsOfTypeAll<EnemyType>())
        {
            if (!Plugin.Instance.enemyTypes.ContainsKey(type.enemyName))
            {
                Plugin.Instance.enemyTypes.Add(type.enemyName, type);
                Plugin.Logger.LogWarning($"Added enemy to list: {type.enemyName}");
            }
        }

        bool isHost = (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost);
        if (!isHost)
        {
            RemoveShopItems();
            return;
        }
        if (!Plugin.Instance.SetAsShopItems.Value)
        {
            RemoveShopItems();
        }
    }

    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPrefix]
    static void AwakePrefix()
    {
        Plugin.Instance.bottleItemList.Clear();
        Plugin.Instance.headItemList.Clear();
        Plugin.Instance.SaveableItemDict.Clear();
    }

    private static void RemoveShopItems()
    {
        foreach (Item item in AssetLoader.items)
        {
            LethalLib.Modules.Items.RemoveShopItem(item);
            Plugin.Logger.LogWarning($"Removing shop item: {item}");
        }
    }
}