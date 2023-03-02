using UnityEngine;
using Verse;

namespace Universum.Utilities {
    /**
     * Sets ExitMapGrid.Color.a = 0 on vacuum maps, allowing full transparency for preset vanilla green overlay present on perimeter of vacuum map
     */
    [HarmonyLib.HarmonyPatch(typeof(ExitMapGrid), "Color", HarmonyLib.MethodType.Getter)]
    public static class ExitMapGrid_Color {
        public static void Postfix(ref Color __result) {
            if (!MapInterface_Notify_SwitchedMap.MapIsSpace) return;
            __result.a = 0;
        }
    }
}
