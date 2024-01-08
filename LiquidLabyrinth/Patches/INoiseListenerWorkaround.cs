using HarmonyLib;
using UnityEngine;

namespace LiquidLabyrinth.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class INoiseListenerWorkaround
    {
        // fr so bad.
        [HarmonyPatch(nameof(RoundManager.PlayAudibleNoise))]
        [HarmonyPostfix]
        static void PlayAudibleNoisePatch(RoundManager __instance,Vector3 noisePosition,ref float noiseRange, float noiseLoudness, int timesPlayedInSameSpot, bool noiseIsInsideClosedShip, int noiseID)
        {
            if (noiseIsInsideClosedShip)
            {
                noiseRange /= 2f;
            }
            int num = Physics.OverlapSphereNonAlloc(noisePosition, noiseRange, __instance.tempColliderResults, 64);
            for (int i = 0; i < num; i++)
            {
                INoiseListener noiseListener;
                if (__instance.tempColliderResults[i].transform.TryGetComponent(out noiseListener))
                {
                    if (noiseIsInsideClosedShip)
                    {
                        GrabbableObject component = __instance.tempColliderResults[i].gameObject.GetComponent<GrabbableObject>();
                        if ((component == null || !component.isInShipRoom) && noiseLoudness < 0.9f)
                        {
                            break;
                        }
                    }
                    noiseListener.DetectNoise(noisePosition, noiseLoudness, timesPlayedInSameSpot, noiseID);
                }
            }
        }
    }
}
