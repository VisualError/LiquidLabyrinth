using HarmonyLib;
using LiquidLabyrinth.Utilities;
using Unity.Netcode;

namespace LiquidLabyrinth.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatches
    {
        [HarmonyPatch((nameof(Terminal.Awake)))]
        [HarmonyPostfix]
        public static void Awake()
        {
            bool isHost = (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost);
            if (!isHost)
            {
                RemoveShopItems();
                return;
            }
            if (!Plugin.Instance.SetAsShopItems.Value)
            {
                RemoveShopItems();
            }
        }
        private static void RemoveShopItems()
        {
            foreach (Item item in AssetLoader.items)
            {
                LethalLib.Modules.Items.RemoveShopItem(item);
                Plugin.Logger.LogWarning($"Removing shop item: {item}");
            }
        }
    }
}
