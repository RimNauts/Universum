using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Universum.Utilities {
    [HarmonyLib.HarmonyPatch(typeof(MapTemperature), "OutdoorTemp", HarmonyLib.MethodType.Getter)]
    public static class MapTemperature_OutdoorTemp {
        public static void Postfix(ref float __result, Map ___map) {
            if (!Cache.allowed_utility(___map, "universum.temperature")) return;
            __result = Cache.temperature(___map);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(MapTemperature), "SeasonalTemp", HarmonyLib.MethodType.Getter)]
    public static class MapTemperature_SeasonalTemp {
        public static void Postfix(ref float __result, Map ___map) {
            if (!Cache.allowed_utility(___map, "universum.temperature")) return;
            __result = Cache.temperature(___map);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(RoomTempTracker), "WallEqualizationTempChangePerInterval")]
    public static class RoomTempTracker_WallEqualizationTempChangePerInterval {
        public static void Postfix(ref float __result, RoomTempTracker __instance) {
            Room room = (Room) typeof(RoomTempTracker).GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (!Cache.allowed_utility(room.Map, "universum.vacuum")) return;
            if (!Cache.allowed_utility(room.Map, "universum.temperature")) return;
            __result *= 0.01f;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(RoomTempTracker), "ThinRoofEqualizationTempChangePerInterval")]
    public static class RoomTempTracker_ThinRoofEqualizationTempChangePerInterval {
        public static void Postfix(ref float __result, RoomTempTracker __instance) {
            Room room = (Room) typeof(RoomTempTracker).GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (!Cache.allowed_utility(room.Map, "universum.vacuum")) return;
            if (!Cache.allowed_utility(room.Map, "universum.temperature")) return;
            __result *= 0.01f;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(RoomTempTracker), "EqualizeTemperature")]
    public static class RoomTempTracker_EqualizeTemperature {
        public static void Postfix(RoomTempTracker __instance) {
            Room room = (Room) typeof(RoomTempTracker).GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (!Cache.allowed_utility(room.Map, "universum.vacuum")) return;
            if (!Cache.allowed_utility(room.Map, "universum.temperature")) return;
            if (room.OpenRoofCount <= 0) return;
            __instance.Temperature = Cache.temperature(room.Map);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(District), "OpenRoofCountStopAt")]
    public static class District_OpenRoofCountStopAt {
        public static void Postfix(int threshold, ref int __result, District __instance) {
            if (!Cache.allowed_utility(__instance.Map, "universum.vacuum")) return;
            IEnumerator<IntVec3> cells = __instance.Cells.GetEnumerator();
            if (__result < threshold && cells != null) {
                TerrainGrid terrainGrid = __instance.Map.terrainGrid;
                while (__result < threshold && cells.MoveNext()) {
                    if (Cache.allowed_utility(terrainGrid.TerrainAt(cells.Current), "universum.vacuum")) __result++;
                }
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Room), "Notify_TerrainChanged")]
    public static class Room_Notify_TerrainChanged {
        public static void Postfix(Room __instance) {
            if (!Cache.allowed_utility(__instance.Map, "universum.vacuum")) return;
            __instance.Notify_RoofChanged();
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(RimWorld.GlobalControls), "TemperatureString")]
    public static class GlobalControls_TemperatureString {
        public static void Postfix(ref string __result) {
            if (!Cache.allowed_utility(Find.CurrentMap, "universum.vacuum")) return;
            if (__result.Contains("Indoors".Translate())) {
                __result = __result.Replace("Indoors".Translate(), "universum.indoors".Translate());
            } else if (__result.Contains("IndoorsUnroofed".Translate())) {
                __result = __result.Replace("IndoorsUnroofed".Translate(), "universum.unroofed".Translate());
            } else if (__result.Contains("Outdoors".Translate().CapitalizeFirst())) {
                __result = __result.Replace("Outdoors".Translate().CapitalizeFirst(), "universum.outdoors".Translate());
            }
        }
    }
}
