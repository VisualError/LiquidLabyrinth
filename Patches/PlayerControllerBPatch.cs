using GameNetcodeStuff;
using HarmonyLib;
using LiquidLabyrinth.ItemHelpers;
using LiquidLabyrinth.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace LiquidLabyrinth.Patches
{
    internal class PlayerControllerBPatch
    {

        [HarmonyPatch(typeof(DeadBodyInfo), nameof(DeadBodyInfo.Start))]
        [HarmonyPostfix]
        static void DeadBodyInfo_AwakePatch(DeadBodyInfo __instance)
        {
            if (__instance.detachedHead && __instance.detachedHeadObject != null)
            {
                Item headItem = AssetLoader.assetsDictionary["assets/liquid labyrinth/headitem.asset"] as Item;
                GameObject obj = GameObject.Instantiate(headItem.spawnPrefab, __instance.transform);
                obj.transform.position = __instance.detachedHeadObject.position;
                obj.transform.rotation = __instance.detachedHeadObject.rotation;
                obj.transform.Rotate(headItem.rotationOffset);
                __instance.detachedHeadObject.gameObject.SetActive(false);
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) obj.GetComponent<NetworkObject>().Spawn();
            }
        }

        /*[ServerRpc(RequireOwnership=false)]
        static void SpawnHead_ServerRpc()
        {

        }*/

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        [HarmonyPrefix]
        static bool StartOfRound_Awake()
        {
            Plugin.Instance.headItemList.Clear();
            return true;
        }

        /*[HarmonyPatch(typeof(DeadBodyInfo), "Start")]
        [HarmonyPrefix]
        static bool DeadBodyInfo_AwakePatch(DeadBodyInfo __instance)
        {
            if (__instance.detachedHead && __instance.detachedHeadObject != null)
            {
                Throwable throwable = __instance.detachedHeadObject.gameObject.AddComponent<Throwable>();
                __instance.detachedHeadObject.gameObject.AddComponent<AudioSource>();
                throwable.itemProperties = Plugin.Instance.item;
                NetworkObject net_Object = __instance.detachedHeadObject.gameObject.AddComponent<NetworkObject>();
                *//*var networkPrefab = new NetworkPrefab
                {
                    Prefab = net_Object.gameObject,
                };*//*
                //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(networkPrefab);
                NetworkManager.Singleton.AddNetworkPrefab(__instance.detachedHeadObject.gameObject);
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) net_Object.Spawn();
            }
            return true;
        }*/

        /*[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpawnDeadBody))]
        [HarmonyPostfix]
        static void SpawnDeadBodyPostfix(int playerId, int deathAnimation)
        {
            if(deathAnimation == 1)
            {
                DeadBodyInfo[] deadBodyInfo = Object.FindObjectsOfType<DeadBodyInfo>();
                if (deadBodyInfo != null && deadBodyInfo[0] != null)
                {
                    foreach(DeadBodyInfo info in deadBodyInfo)
                    {
                        if(info.playerObjectId == playerId)
                        {
                            Throwable throwable = info.detachedHeadObject.gameObject.AddComponent<Throwable>();
                            info.detachedHeadObject.gameObject.AddComponent<AudioSource>();
                            throwable.itemProperties = Plugin.Instance.item;
                            NetworkObject net_Object = info.detachedHeadObject.gameObject.AddComponent<NetworkObject>();
                            NetworkManager.Singleton.AddNetworkPrefab(net_Object.gameObject);
                            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) net_Object.Spawn();
                            return;
                        }
                    }
                }
            }
        }*/
    }
}