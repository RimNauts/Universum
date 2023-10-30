using HarmonyLib;
using System.Reflection;

namespace Universum.World.Patch {
    public class SettleInEmptyTileUtility {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(SettleInEmptyTileUtility_Settle)).Patch();
        }
    }

    [HarmonyPatch]
    static class SettleInEmptyTileUtility_Settle {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.SettleInEmptyTileUtility:Settle");

        public static bool Prefix(RimWorld.Planet.Caravan caravan) {
            ObjectHolder objectHolder = ObjectHolderCache.Get(caravan.Tile);
            if (objectHolder == null) return true;

            objectHolder.Settle(caravan);

            return false;
        }
    }
}
