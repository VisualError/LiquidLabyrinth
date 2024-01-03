using HarmonyLib;
using LiquidLabyrinth.ItemHelpers;
using System.Reflection;
namespace LiquidLabyrinth.Patches
{
    [HarmonyPatch(typeof(GrabbableObject))]
    internal class GrabbableObjectPatch
    {
        [HarmonyPatch(nameof(GrabbableObject.FallToGround))]
        [HarmonyPrefix]
        static bool FallToGroundPatch(GrabbableObject __instance)
        {
            return __instance.GetType().BaseType.BaseType != typeof(GrabbableRigidbody);
        }

        [HarmonyPatch(nameof(GrabbableObject.EnablePhysics))]
        [HarmonyPrefix]
        static bool EnablePhysics(GrabbableObject __instance,bool enable)
        {
            
            if(__instance.GetType().BaseType.BaseType == typeof(GrabbableRigidbody) && __instance is GrabbableRigidbody rigid && rigid != null)
            {
                Plugin.Logger.LogWarning($"replaced physics called: {enable} ({Assembly.GetCallingAssembly().GetName().Name})");
                rigid.EnableColliders(enable);
                // Do this before setting kinematic to enabled.
                if (rigid.isHeldByEnemy && !enable)
                {
                    Plugin.Logger.LogWarning($"Was held by enemy. Trying to fix values");
                    rigid.fallTime = 1f;
                    rigid.startFallingPosition = rigid.transform.position;
                    rigid.targetFloorPosition = rigid.transform.position;
                    rigid.floorYRot = (int)rigid.transform.localEulerAngles.y;
                }
                // enable rigidbody
                rigid.rb.isKinematic = !enable;
                return false;
            }
            return true;
        }
    }
}
