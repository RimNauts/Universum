using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace Universum.World.Patch {
    public class GetOrGenerateMapUtility {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(GetOrGenerateMapUtility_GetOrGenerateMap)).Patch();
        }
    }

    [HarmonyPatch]
    static class GetOrGenerateMapUtility_GetOrGenerateMap {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() {
            var type = AccessTools.TypeByName("Verse.GetOrGenerateMapUtility");
            if (type == null) return null;

            var method = AccessTools.Method(type, "GetOrGenerateMap", new Type[] { typeof(int), typeof(IntVec3), typeof(RimWorld.WorldObjectDef) });
            if (method == null) return null;

            return method;
        }

        public static void Postfix(int tile, IntVec3 size, RimWorld.WorldObjectDef suggestedMapParentDef, ref Map __result) {
            if (__result == null) return;

            ObjectHolder objectHolder = ObjectHolderCache.Get(tile);
            if (objectHolder == null || objectHolder.Faction != null) return;

            objectHolder.SetFaction(RimWorld.Faction.OfPlayer);
        }
    }
}
