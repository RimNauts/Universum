using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Universum.Utilities {
    [HarmonyLib.HarmonyPatch(typeof(Verse.MapTemperature), "OutdoorTemp", HarmonyLib.MethodType.Getter)]
    public static class MapTemperature_OutdoorTemp {
        public static void Postfix(ref float __result, Verse.Map ___map) {
            if (!Cache.allowed_utility(___map, "Universum.temperature")) return;
            __result = Cache.temperature(___map);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Verse.MapTemperature), "SeasonalTemp", HarmonyLib.MethodType.Getter)]
    public static class MapTemperature_SeasonalTemp {
        public static void Postfix(ref float __result, Verse.Map ___map) {
            if (!Cache.allowed_utility(___map, "Universum.temperature")) return;
            __result = Cache.temperature(___map);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Verse.RoomTempTracker), "WallEqualizationTempChangePerInterval")]
    public static class RoomTempTracker_WallEqualizationTempChangePerInterval {
        public static void Postfix(ref float __result, Verse.RoomTempTracker __instance) {
            Verse.Room room = (Verse.Room) typeof(Verse.RoomTempTracker).GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (!Cache.allowed_utility(room.Map, "Universum.vacuum")) return;
            if (!Cache.allowed_utility(room.Map, "Universum.temperature")) return;
            __result *= 0.01f;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Verse.RoomTempTracker), "ThinRoofEqualizationTempChangePerInterval")]
    public static class RoomTempTracker_ThinRoofEqualizationTempChangePerInterval {
        public static void Postfix(ref float __result, Verse.RoomTempTracker __instance) {
            Verse.Room room = (Verse.Room) typeof(Verse.RoomTempTracker).GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (!Cache.allowed_utility(room.Map, "Universum.vacuum")) return;
            if (!Cache.allowed_utility(room.Map, "Universum.temperature")) return;
            __result *= 0.01f;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Verse.RoomTempTracker), "EqualizeTemperature")]
    public static class RoomTempTracker_EqualizeTemperature {
        public static void Postfix(Verse.RoomTempTracker __instance) {
            Verse.Room room = (Verse.Room) typeof(Verse.RoomTempTracker).GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            if (!Cache.allowed_utility(room.Map, "Universum.vacuum")) return;
            if (!Cache.allowed_utility(room.Map, "Universum.temperature")) return;
            if (room.OpenRoofCount <= 0) return;
            __instance.Temperature = Cache.temperature(room.Map);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Verse.District), "OpenRoofCountStopAt")]
    public static class District_OpenRoofCountStopAt {
        public static void Postfix(int threshold, ref int __result, Verse.District __instance) {
            if (!Cache.allowed_utility(__instance.Map, "Universum.vacuum")) return;
            IEnumerator<Verse.IntVec3> cells = __instance.Cells.GetEnumerator();
            if (__result < threshold && cells != null) {
                Verse.TerrainGrid terrainGrid = __instance.Map.terrainGrid;
                while (__result < threshold && cells.MoveNext()) {
                    if (Cache.allowed_utility(terrainGrid.TerrainAt(cells.Current), "Universum.vacuum")) __result++;
                }
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Verse.Room), "Notify_TerrainChanged")]
    public static class Room_Notify_TerrainChanged {
        public static void Postfix(Verse.Room __instance) {
            if (!Cache.allowed_utility(__instance.Map, "Universum.vacuum")) return;
            __instance.Notify_RoofChanged();
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(RimWorld.GlobalControls), "TemperatureString")]
    public static class GlobalControls_TemperatureString {
        public static void Postfix(ref string __result) {
            if (!Cache.allowed_utility(Verse.Find.CurrentMap, "Universum.vacuum")) return;
            if (__result.Contains("Indoors".Translate())) {
                __result = __result.Replace("Indoors".Translate(), "Universum.indoors".Translate());
            } else if (__result.Contains("IndoorsUnroofed".Translate())) {
                __result = __result.Replace("IndoorsUnroofed".Translate(), "Universum.unroofed".Translate());
            } else if (__result.Contains("Outdoors".Translate().CapitalizeFirst())) {
                __result = __result.Replace("Outdoors".Translate().CapitalizeFirst(), "Universum.outdoors".Translate());
            }
        }
    }
}
