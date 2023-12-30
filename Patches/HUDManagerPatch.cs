using HarmonyLib;
using LiquidLabyrinth.Utilities;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace LiquidLabyrinth.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    class HUDManagerPatch
    {
        [HarmonyPatch(nameof(HUDManager.DisplayNewScrapFound))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            var instruct = new List<CodeInstruction>(instructions);
            int i = 0;
            foreach(CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Contains("Destroy") && instruct[i-1] != null && instruct[i-1].opcode == OpCodes.Callvirt && instruct[i-1].operand.ToString().Contains("Collider"))
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OtherUtils), "TryDestroyRigidBody", new System.Type[] { typeof(GameObject) }));
                    yield return new CodeInstruction(OpCodes.Pop);
                    Plugin.Logger.LogWarning("INSERTED RIGIDBODY DESTROY ON DisplayNewScrapFound");
                }
                else
                {
                    yield return instruction;
                }
                i++;
            }
        }
    }
}
