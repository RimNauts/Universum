using HarmonyLib;
using System.Reflection;

namespace Universum.World.Patch {
    public class WorldGrid {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(WorldGrid_TraversalDistanceBetween)).Patch();
        }
    }

    [HarmonyPatch]
    static class WorldGrid_TraversalDistanceBetween {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldGrid:TraversalDistanceBetween");

        public static void Postfix(int start, int end, bool passImpassable, int maxDist, ref int __result) {
            bool fromOrbit = ObjectHolderCache.Exists(start);
            if (fromOrbit) {
                __result = 20;
                return;
            }

            bool toOrbit = ObjectHolderCache.Exists(end);
            if (toOrbit) __result = 100;
        }
    }
}
