using HarmonyLib;
using Steamworks;
using Steamworks.Data;

namespace LiquidLabyrinth.Labyrinth.LiquidAPI_Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class LobbyDataSet
    {
        [HarmonyPatch(nameof(GameNetworkManager.SteamMatchmaking_OnLobbyCreated))]
        [HarmonyPostfix]
        static void SteamMatchmaking_OnLobbyCreated(GameNetworkManager __instance, Result result, ref Lobby lobby)
        {
            lobby.SetData("a", "b"); // todo JSON utility.
        }
    }
}
