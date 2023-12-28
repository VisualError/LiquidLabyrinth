using HarmonyLib;

namespace LiquidLabyrinth.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    class StartOfRoundPatch
    {
        [HarmonyPatch(nameof(StartOfRound.Awake))]
        static bool Prefix()
        {
            LiquidLabyrinthBase.bottlesAdded = 1;
            return true;
        }
    }
}
