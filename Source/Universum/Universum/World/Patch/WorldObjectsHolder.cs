using HarmonyLib;
using System.Reflection;

namespace Universum.World.Patch {
    public class WorldObjectsHolder {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(WorldObjectsHolder_AddToCache)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldObjectsHolder_RemoveFromCache)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldObjectsHolder_Recache)).Patch();
        }
    }

    [HarmonyPatch]
    static class WorldObjectsHolder_AddToCache {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldObjectsHolder:AddToCache");

        public static void Prefix(RimWorld.Planet.WorldObject o) {
            if (o is ObjectHolder objectHolder) ObjectHolderCache.Add(objectHolder);
        }
    }

    [HarmonyPatch]
    static class WorldObjectsHolder_RemoveFromCache {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldObjectsHolder:RemoveFromCache");

        public static void Prefix(RimWorld.Planet.WorldObject o) {
            if (o is ObjectHolder objectHolder) ObjectHolderCache.Remove(objectHolder);
        }
    }

    [HarmonyPatch]
    static class WorldObjectsHolder_Recache {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldObjectsHolder:Recache");

        public static void Prefix() {
            ObjectHolderCache.Clear();
        }
    }
}
