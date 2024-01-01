using LiquidLabyrinth.ItemHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LiquidLabyrinth.Utilities
{
    internal class AssetLoader
    {
        internal static Dictionary<string, Object> assetsDictionary = new Dictionary<string, Object>();

        internal static void LoadAssetBundles()
        {
            var bundle = AssetBundle.LoadFromMemory(Properties.Resources.liquidlabyrinth);
            if(bundle == null)
            {
                Plugin.Logger.LogWarning($"Assetbundle could not be loaded!");
                return;
            }
            foreach (var assetName in bundle.GetAllAssetNames())
            {
                if (!Path.GetExtension(assetName).Contains("asset")) continue;
                Object asset = bundle.LoadAsset<Object>(assetName);
                if(asset == null)
                {
                    Plugin.Logger.LogWarning($"Asset {assetName} could not be loaded because it is not an Object!");
                    continue;
                }
                assetsDictionary.Add(assetName, asset);
                if (asset is Item item)
                {
                    if (!item.isScrap || Plugin.Instance.SetAsShopItems.Value)
                    {
                        LethalLib.Modules.Items.RegisterShopItem(item, -1);
                        Plugin.Logger.LogWarning($"Added Shop Item: {assetName}");
                        if (!Plugin.Instance.SetAsShopItems.Value)
                        {
                            continue;
                        }
                    }
                    int rarity = item.GetType() != typeof(PotionBottle) ? 60 : Plugin.Instance.BottleRarity.Value; // need to turn this into a dictionary or something. this sucks lol
                    LethalLib.Modules.Items.RegisterScrap(item, rarity, LethalLib.Modules.Levels.LevelTypes.All);
                    Plugin.Logger.LogWarning($"Added Scrap Item: {assetName}");

                    if (item.spawnPrefab.TryGetComponent(out NetworkObject obj) && obj != null)
                    {
                        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
                        Plugin.Logger.LogWarning($"NetworkPrefab for {assetName} loaded!");
                    }
                }
            }
        }
    }
}
