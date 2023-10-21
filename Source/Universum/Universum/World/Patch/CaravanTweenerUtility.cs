using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Universum.World.Patch {
    public class CaravanTweenerUtility {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(CaravanTweenerUtility_PatherTweenedPosRoot)).Patch();
        }
    }

    [HarmonyPatch]
    static class CaravanTweenerUtility_PatherTweenedPosRoot {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.CaravanTweenerUtility:PatherTweenedPosRoot");

        public static bool Prefix(RimWorld.Planet.Caravan caravan, ref Vector3 __result) {
            ObjectHolder objectHolder = ObjectHolderCache.Get(caravan.Tile);
            if (objectHolder == null) return true;

            __result = objectHolder.DrawPos;

            return false;
        }
    }
}
