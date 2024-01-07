using HarmonyLib;
using LiquidLabyrinth.Utilities;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using GameNetcodeStuff;
using LiquidLabyrinth.ItemHelpers;

namespace LiquidLabyrinth.Patches;

internal class PlayerControllerBPatch
{

    [HarmonyPatch(typeof(DeadBodyInfo), nameof(DeadBodyInfo.Start))]
    [HarmonyPostfix]
    static void DeadBodyInfo_AwakePatch(DeadBodyInfo __instance)
    {
        if (__instance.detachedHead && __instance.detachedHeadObject != null)
        {
            __instance.detachedHeadObject.gameObject.SetActive(false);
            if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)) return;
            Item? headItem = AssetLoader.assetsDictionary["assets/liquid labyrinth/headitem.asset"] as Item;
            if (headItem == null) return;
            GameObject obj = GameObject.Instantiate(headItem.spawnPrefab, __instance.transform);
            obj.transform.position = __instance.detachedHeadObject.position;
            obj.transform.rotation = __instance.detachedHeadObject.rotation;
            obj.transform.Rotate(headItem.rotationOffset);
            obj.GetComponent<NetworkObject>().Spawn();
        }
    }


    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlaceGrabbableObject))]
    [HarmonyPostfix]
    static void PlaceGrabbableObjectPostfix(PlayerControllerB __instance, ref GrabbableObject placeObject)
    {
        if(placeObject is GrabbableRigidbody rigid && rigid != null) 
        {
            placeObject.EnablePhysics(false);
            rigid.EnableColliders(true);
            if (__instance.IsOwner) rigid.PlayDropSFX();
        }
    }
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlaceGrabbableObject))]
    [HarmonyPrefix]
    static void PlaceGrabbableObjectPrefix(PlayerControllerB __instance, ref GrabbableObject placeObject)
    {
        if (placeObject is GrabbableRigidbody rigid && rigid != null)
        {
            if (__instance.IsOwner) rigid.net_Placed.Value = true;
        }
    }

    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue))]
    [HarmonyPostfix]
    static void SetScrapValue(GrabbableObject __instance)
    {
        if ((NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)) return;
        ScanNodeProperties componentInChildren = __instance.gameObject.GetComponentInChildren<ScanNodeProperties>();
        if (componentInChildren != null && __instance is PotionBottle bottle && bottle != null)
        {
            if (bottle.Liquid == null)
            {
                Plugin.Logger.LogWarning("Screams");
                return;
            }
            componentInChildren.subText += $"\n{bottle.Liquid.Name}";
        }
    }
}