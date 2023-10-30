using HarmonyLib;
using System.Reflection;
using System.Text;
using Verse;

namespace Universum.World.Patch {
    public class TileFinder {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(TileFinder_IsValidTileForNewSettlement)).Patch();
        }
    }

    [HarmonyPatch]
    static class TileFinder_IsValidTileForNewSettlement {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.TileFinder:IsValidTileForNewSettlement");

        public static void Postfix(int tile, StringBuilder reason, ref bool __result) {
            if (__result || reason == null || !ObjectHolderCache.Exists(tile)) return;

            if (Find.WorldObjects.SettlementBaseAt(tile) != null) return;

            if (!reason.ToString().Contains("TileOccupied".Translate())) return;

            __result = true;
        }
    }
}
