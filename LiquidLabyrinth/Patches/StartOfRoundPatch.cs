using HarmonyLib;
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
        foreach (EnemyType type in Resources.FindObjectsOfTypeAll<EnemyType>())
        {
            if (!Plugin.Instance.enemyTypes.ContainsKey(type.enemyName))
            {
                Plugin.Instance.enemyTypes.Add(type.enemyName, type);
                Plugin.Logger.LogWarning($"Added enemy to list: {type.enemyName}");
            }
        }
    }
}