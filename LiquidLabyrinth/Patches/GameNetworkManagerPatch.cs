using HarmonyLib;
using LiquidLabyrinth.ItemHelpers;
using UnityEngine;

namespace LiquidLabyrinth.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
class GameNetworkManagerPatch
{
    // I should remove this but i feel like i shouldnt
    [HarmonyPatch("SaveItemsInShip")]
    static void Postfix()
    {
        GrabbableObject[] array = Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (array == null || array.Length == 0)
        {
            return;
        }
        foreach(GrabbableObject item in array)
        {
            if (item.itemProperties.saveItemVariable && item.GetType().Equals(typeof(PotionBottle)))
            {
                //Plugin.Logger.Log(BepInEx.Logging.LogLevel.All, "FOUND BOTTLE HAHA");
                //CoroutineHandler.Instance.NewCoroutine(SaveUtils.ProcessQueueAfterDelay<PotionBottle>(item.GetType(), 0.5f));
            }
        }
    }
}