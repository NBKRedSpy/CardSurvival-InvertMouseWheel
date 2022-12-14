using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace InvertMouseWheel
{
    [HarmonyPatch(typeof(MouseWheelSensitivity), "Update")]
    public static class MousewheelSensitivity_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            //The new -1 multiplier for the mouse wheel direction indicator.
            List<CodeInstruction> multiplyNegativeInstructions = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_R4, -1f),
                new CodeInstruction(OpCodes.Mul)
            };


            //--- Methods to search for
            MethodInfo horizontalPositionGetProperty = AccessTools.PropertyGetter(typeof(UnityEngine.UI.ScrollRect), 
                nameof(UnityEngine.UI.ScrollRect.horizontalNormalizedPosition));

            MethodInfo getAxisMethod = AccessTools.Method(typeof(UnityEngine.Input), nameof(UnityEngine.Input.GetAxis),
                new[] { typeof(string) });

            FieldInfo sensitivityMultiplierField = AccessTools.Field(typeof(MouseWheelSensitivity), nameof(MouseWheelSensitivity.SensitivityMultiplier));


            //--- Code modification
            List<CodeInstruction> newCode = new(new CodeMatcher(instructions)
                .MatchForward(true, new CodeMatch(OpCodes.Callvirt, horizontalPositionGetProperty))
                .ThrowIfNotMatch("Did not find horizontal scrollbar section")

                .MatchForward(true, new CodeMatch(OpCodes.Call, getAxisMethod))
                .ThrowIfNotMatch("Did not find first GetAxis Call")
                .Advance(1)
                .Insert(multiplyNegativeInstructions)

                .MatchForward(true, new CodeMatch(OpCodes.Call, getAxisMethod))
                .ThrowIfNotMatch("Did not find second GetAxis Call")
                .Advance(1)
                .Insert(multiplyNegativeInstructions)

                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldfld, sensitivityMultiplierField),
                    new CodeMatch(OpCodes.Mul)
                    )
                .ThrowIfNotMatch("Did not find first Sensitivity Multiplier")
                .Advance(1)
                .Insert(multiplyNegativeInstructions)

                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldfld, sensitivityMultiplierField),
                    new CodeMatch(OpCodes.Mul)
                    )
                .ThrowIfNotMatch("Did not find second Sensitivity Multiplier")
                .Advance(1)
                .Insert(multiplyNegativeInstructions)
                .InstructionEnumeration()
                ); 

            return newCode;



        }
    }
}
