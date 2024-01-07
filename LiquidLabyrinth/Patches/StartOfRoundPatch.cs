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
    }

    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPrefix]
    static bool AwakePrefix()
    {
        Plugin.Instance.SaveableItemDict.Clear();
        return true;
    }
}