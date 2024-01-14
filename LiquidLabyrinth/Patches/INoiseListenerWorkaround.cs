using HarmonyLib;
using UnityEngine;

namespace LiquidLabyrinth.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class INoiseListenerWorkaround
    {

        static Collider[] itemColliderResults = new Collider[20];
        // fr so bad.
        [HarmonyPatch(nameof(RoundManager.PlayAudibleNoise))]
        [HarmonyPostfix]
        static void PlayAudibleNoisePatch(RoundManager __instance,Vector3 noisePosition,ref float noiseRange, float noiseLoudness, int timesPlayedInSameSpot, bool noiseIsInsideClosedShip, int noiseID)
        {
            if (noiseIsInsideClosedShip)
            {
                noiseRange /= 2f;
            }
            int num = Physics.OverlapSphereNonAlloc(noisePosition, noiseRange, itemColliderResults, 64);
            for (int i = 0; i < num; i++)
            {
                INoiseListener noiseListener;
                if (itemColliderResults[i].transform.TryGetComponent(out noiseListener))
                {
                    if (noiseIsInsideClosedShip)
                    {
                        GrabbableObject component = itemColliderResults[i].gameObject.GetComponent<GrabbableObject>();
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
