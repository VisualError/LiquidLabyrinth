using HarmonyLib;
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
        Plugin.Instance.bottleItemList.Clear();
        Plugin.Instance.headItemList.Clear();
    }

    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPrefix]
    static void AwakePrefix()
    {
        if (!Plugin.Instance.SetAsShopItems.Value || !(NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)) 
        {
            foreach(LethalLib.Modules.Items.ShopItem shopItem in LethalLib.Modules.Items.shopItems)
            {
                LethalLib.Modules.Items.RemoveShopItem(shopItem.item);
            }
        }
    }
}