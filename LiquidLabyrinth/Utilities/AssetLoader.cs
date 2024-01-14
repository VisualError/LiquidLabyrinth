using LiquidLabyrinth.ItemHelpers;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LiquidLabyrinth.Utilities;

internal class AssetLoader
{
    internal static Dictionary<string, Object> assetsDictionary = new Dictionary<string, Object>();
    internal static List<Item> items = new List<Item>();
    internal static AssetBundle? bundle;
    internal static void LoadAssetBundles()
    {
        bundle = AssetBundle.LoadFromMemory(Properties.Resources.liquidlabyrinth);
        if(bundle == null)
        {
            Plugin.Logger.LogWarning($"Assetbundle could not be loaded!");
            return;
        }
        foreach (var assetName in bundle.GetAllAssetNames())
        {
            Object asset = bundle.LoadAsset<Object>(assetName);
            if(asset == null)
            {
                Plugin.Logger.LogWarning($"Asset {assetName} could not be loaded because it is not an Object!");
                continue;
            }
            assetsDictionary.Add(assetName, asset); // Add any loaded asset into the assetsDictionary.
            Plugin.Logger.LogInfo($"Added: {assetName} to assetDictionary");
            if (asset is Item item) // Only register items as items.
            {
                int rarity = item.GetType() != typeof(PotionBottle) ? 20 : Plugin.Instance.BottleRarity.Value; // need to turn this into a dictionary or something. this sucks lol
                LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.All);
                Plugin.Logger.LogWarning($"Added Scrap Item: {assetName}");
                if (item.spawnPrefab.TryGetComponent(out NetworkObject obj) && obj != null)
                {
                    
                    LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
                    Plugin.Logger.LogWarning($"NetworkPrefab for {assetName} loaded!");
                }
                if (!items.Contains(item)) items.Add(item);
            }
        }
    }
}