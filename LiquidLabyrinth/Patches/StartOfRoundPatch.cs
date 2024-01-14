using BepInEx;
using HarmonyLib;
using LethalLib.Modules;
using LiquidLabyrinth.Utilities;
using System;
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
        foreach (EnemyType enemyType in Resources.FindObjectsOfTypeAll<EnemyType>())
        {
            Type enemyAIType = enemyType.enemyPrefab.GetComponent<EnemyAI>().GetType();
            if (!Plugin.Instance.enemyTypes.ContainsKey(enemyAIType))
            {
                Plugin.Instance.enemyTypes.Add(enemyAIType, enemyType);
                Plugin.Logger.LogWarning($"Added enemy to list: {enemyType.enemyName} ({enemyAIType})");
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

    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPrefix]
    static void SpawnNetworkHandler()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            var networkHandlerHost = GameObject.Instantiate(GameNetworkManagerPatch.networkPrefab, Vector3.zero, Quaternion.identity); // I need to put all my network classes in a single class to stop using public variables jesussssssssss
            networkHandlerHost.GetComponent<NetworkObject>().Spawn();
        }
    }
}