using System.Linq;

namespace Universum.Utilities {
    /**
     * Masks biome tile as ocean to apply ocean material when drawing planet.
     */
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.BiomeDef), "DrawMaterial", HarmonyLib.MethodType.Getter)]
    public static class BiomeDef_DrawMaterial {
        public static void Prefix(ref RimWorld.BiomeDef __instance) {
            if (Cache.allowed_utility(__instance, "Universum.ocean_masking")) __instance = RimWorld.BiomeDefOf.Ocean;
        }
    }

    /**
     * Removes yellow material from planet tile used to show location of last map clicked on.
     * Without this it just added a yellow circle in the ocean.
     */
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.Planet.WorldLayer_CurrentMapTile), "Tile", HarmonyLib.MethodType.Getter)]
    public static class WorldLayer_CurrentMapTile_Tile {
        public static void Postfix(ref int __result) {
            if (__result == -1) return;
            if (Cache.allowed_utility(Verse.Find.World.grid.tiles.ElementAt(__result).biome, "Universum.ocean_masking")) __result = -1;
        }
    }
}
