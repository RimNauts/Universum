using HarmonyLib;
using System.Reflection;

namespace Universum.Patch {
    public class MemoryUtility {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(MemoryUtility_ClearAllMapsAndWorld)).Patch();
        }
    }

    [HarmonyPatch]
    static class MemoryUtility_ClearAllMapsAndWorld {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("Verse.Profile.MemoryUtility:ClearAllMapsAndWorld");

        public static void Prefix() {
            if (Game.MainLoop.instance == null) return;
            Game.MainLoop.instance.Destroy();
            Game.MainLoop.instance = null;
        }
    }
}
