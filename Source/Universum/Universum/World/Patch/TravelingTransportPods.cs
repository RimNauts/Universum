using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Universum.World.Patch {
    public class TravelingTransportPods {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(TravelingTransportPods_Start)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(TravelingTransportPods_End)).Patch();
        }
    }

    [HarmonyPatch]
    static class TravelingTransportPods_Start {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.TravelingTransportPods:get_Start");

        public static bool Prefix(RimWorld.Planet.TravelingTransportPods __instance, ref Vector3 __result) {
            ObjectHolder objectHolder = ObjectHolderCache.Get(__instance.initialTile);
            if (objectHolder == null) return true;

            __result = objectHolder.DrawPos;

            return false;
        }
    }

    [HarmonyPatch]
    static class TravelingTransportPods_End {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.TravelingTransportPods:get_End");

        public static bool Prefix(RimWorld.Planet.TravelingTransportPods __instance, ref Vector3 __result) {
            ObjectHolder objectHolder = ObjectHolderCache.Get(__instance.destinationTile);
            if (objectHolder == null) return true;

            __result = objectHolder.DrawPos;

            return false;
        }
    }
}
