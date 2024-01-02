using HarmonyLib;
using LiquidLabyrinth.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace LiquidLabyrinth.Patches;

// Ill remove this when terminal sync gets added to lethallib.
[HarmonyPatch(typeof(Terminal))]
class TerminalPatch
{
    [HarmonyPatch(nameof(Terminal.Awake))]
    [HarmonyPrefix]
    [HarmonyBefore("evaisa.lethallib")]
    static bool Terminal_Awake(Terminal __instance)
    {
        var buyKeyword = __instance.terminalNodes.allKeywords.First(keyword => keyword.word == "buy");
        foreach (KeyValuePair<string, Object> obj in AssetLoader.assetsDictionary)
        {
            Item item = obj.Value as Item;
            if (Plugin.Instance.SetAsShopItems.Value)
            {
                LethalLib.Modules.Items.ShopItem shopItem = new LethalLib.Modules.Items.ShopItem(item, null, null, null, -1); // always -1 for my case anyway.
                shopItem.modName = Assembly.GetExecutingAssembly().GetName().Name;
                var shopItemExists = LethalLib.Modules.Items.shopItems.Any(si => si.item == item);
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    if (!shopItemExists)
                    {
                        LethalLib.Modules.Items.shopItems.Add(shopItem);
                        Plugin.Logger.LogWarning($"Fix applied *added*: {shopItem.item.itemName}");
                    }
                }
                else
                {
                    if (shopItemExists)
                    {
                        var shopItemToRemove = LethalLib.Modules.Items.shopItems.FirstOrDefault(si => si.item == item);
                        LethalLib.Modules.Items.shopItems.Remove(shopItemToRemove);
                        Plugin.Logger.LogWarning($"Fix applied *removed*: {shopItemToRemove.item.itemName}");
                        RemoveKeywords(__instance, item, buyKeyword);
                    }
                }
            }
            else
            {
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    RemoveKeywords(__instance, item, buyKeyword);
                }
            }
        }
        return true;
    }

    static void RemoveKeywords(Terminal __instance, Item item, TerminalKeyword buyKeyword)
    {
        var withoutKeyword = __instance.terminalNodes.allKeywords.Where(keyword => keyword.word != item.itemName.ToLowerInvariant().Replace(" ", "-"));
        __instance.terminalNodes.allKeywords = withoutKeyword.ToArray();
        var withoutNouns = buyKeyword.compatibleNouns.Where(noun => noun.noun.word != item.itemName.ToLowerInvariant().Replace(" ", "-"));
        buyKeyword.compatibleNouns = withoutNouns.ToArray();
        Plugin.Logger.LogWarning($"Fix applied removed keywords for: {item.itemName}");
    }
}