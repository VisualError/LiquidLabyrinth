using GameNetcodeStuff;
using HarmonyLib;
using LiquidLabyrinth.Utilities;

namespace LiquidLabyrinth.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch(nameof(PlayerControllerB.Start))]
        [HarmonyPostfix]
        public static void Awake_Prefix(PlayerControllerB __instance)
        {
            OtherUtils.SetTagRecursively(__instance.gameObject, "Player");
        }
    }
}