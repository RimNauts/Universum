using HarmonyLib;
using System.Reflection;

namespace Universum.World.Patch {
    public class WorldObjectSelectionUtility {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(WorldObjectSelectionUtility_HiddenBehindTerrainNow)).Patch();
        }
    }

    [HarmonyPatch]
    static class WorldObjectSelectionUtility_HiddenBehindTerrainNow {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldObjectSelectionUtility:HiddenBehindTerrainNow");

        public static bool Prefix(RimWorld.Planet.WorldObject o, ref bool __result) {
            if (o is ObjectHolder objectHolder) {
                __result = objectHolder.hideIcon;
                return false;
            }

            return true;
        }
    }
}
