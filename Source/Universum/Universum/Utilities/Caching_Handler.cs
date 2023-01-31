using System.Collections.Generic;
using Verse;

namespace Universum.Utilities {
    /**
     * Keeps all the cache collections and their methods.
     */
    public class Caching_Handler : GameComponent {
        public Dictionary<string, bool> utilities;
        public Dictionary<int, Dictionary<string, bool>> map_utilities;
        public Dictionary<string, Dictionary<string, bool>> biome_utilities;
        public Dictionary<string, Dictionary<string, bool>> terrain_utilities;
        public Dictionary<string, Dictionary<string, bool>> gene_utilities;
        public Dictionary<int, float> map_temperature;
        public Dictionary<int, Vacuum_Protection> pawn_protection_level;

        public Caching_Handler(Game game) : base() {
            // initilize empty caches
            clear();
            Cache.caching_handler = this;
        }

        public bool allowed_utility(string utility) {
            if (utilities.TryGetValue(utility, out var value)) {
                return value;
            }
            var toggle = Settings.utility_turned_on(utility);
            utilities.Add(utility, toggle);
            return toggle;
        }

        public bool allowed_utility(Map map, string utility) {
            if (map == null) return false;
            // branch if not enabled in settings
            if (!allowed_utility(utility)) return false;
            // branch if map is cached
            if (map_utilities.TryGetValue(map.uniqueID, out var utilities)) {
                // branch if utility is cached
                if (utilities.TryGetValue(utility, out var cached_property_value)) return cached_property_value;
            } else map_utilities.Add(map.uniqueID, new Dictionary<string, bool>());
            // get value and cache result
            var property_value = Biome.Handler.get_properties(map.Biome).allowed_utilities.Contains(utility);
            map_utilities[map.uniqueID].Add(utility, property_value);
            return property_value;
        }

        public bool allowed_utility(RimWorld.BiomeDef biome, string utility) {
            if (biome == null) return false;
            // branch if not enabled in settings
            if (!allowed_utility(utility)) return false;
            // branch if map is cached
            if (biome_utilities.TryGetValue(biome.defName, out var utilities)) {
                // branch if utility is cached
                if (utilities.TryGetValue(utility, out var cached_property_value)) return cached_property_value;
            } else biome_utilities.Add(biome.defName, new Dictionary<string, bool>());
            // get value and cache result
            var property_value = Biome.Handler.get_properties(biome).allowed_utilities.Contains(utility);
            biome_utilities[biome.defName].Add(utility, property_value);
            return property_value;
        }

        public bool allowed_utility(TerrainDef terrain, string utility) {
            if (terrain == null) return false;
            // branch if not enabled in settings
            if (!allowed_utility(utility)) return false;
            // branch if map is cached
            if (terrain_utilities.TryGetValue(terrain.defName, out var utilities)) {
                // branch if utility is cached
                if (utilities.TryGetValue(utility, out var cached_property_value)) return cached_property_value;
            } else terrain_utilities.Add(terrain.defName, new Dictionary<string, bool>());
            // get value and cache result
            var property_value = Terrain.Handler.get_properties(terrain).allowed_utilities.Contains(utility);
            terrain_utilities[terrain.defName].Add(utility, property_value);
            return property_value;
        }

        public bool allowed_utility(GeneDef gene, string utility) {
            if (!ModsConfig.BiotechActive || gene == null) return false;
            // branch if not enabled in settings
            if (!allowed_utility(utility)) return false;
            // branch if map is cached
            if (gene_utilities.TryGetValue(gene.defName, out var utilities)) {
                // branch if utility is cached
                if (utilities.TryGetValue(utility, out var cached_property_value)) return cached_property_value;
            } else gene_utilities.Add(gene.defName, new Dictionary<string, bool>());
            // get value and cache result
            var property_value = Gene.Handler.get_properties(gene).allowed_utilities.Contains(utility);
            gene_utilities[gene.defName].Add(utility, property_value);
            return property_value;
        }

        public float temperature(Map map) {
            if (map == null) return 0.0f;
            // branch if map is cached
            if (map_temperature.TryGetValue(map.uniqueID, out var temp)) return temp;
            // get value and cache result
            var property_value = Biome.Handler.get_properties(map.Biome).temperature;
            map_temperature.Add(map.uniqueID, property_value);
            return property_value;
        }

        public Vacuum_Protection spacesuit_protection(Pawn pawn) {
            if (pawn == null) return Vacuum_Protection.None;
            // branch if pawn is cached
            if (pawn_protection_level.TryGetValue(pawn.thingIDNumber, out var protection)) return protection;
            // get value and cache result
            Vacuum_Protection value = Vacuum_Protection.None;
            if (pawn.RaceProps.IsMechanoid || !pawn.RaceProps.IsFlesh || (pawn.def.tradeTags?.Contains("AnimalInsectSpace") ?? false)) {
                value = Vacuum_Protection.All;
            } else if (pawn.apparel == null) {
                value = Vacuum_Protection.None;
            } else {
                bool helmet = false;
                bool suit = false;
                // check genes
                if (ModsConfig.BiotechActive) {
                    List<Verse.Gene> genes = pawn.genes.GenesListForReading;
                    foreach (Verse.Gene gene in genes) {
                        if (Cache.allowed_utility(gene.def, "Universum.vacuum_suffocation_protection")) helmet = true;
                        if (Cache.allowed_utility(gene.def, "Universum.vacuum_decompression_protection")) suit = true;
                        if (helmet && suit) break;
                    }
                }
                // check apparel
                if (helmet && suit) {
                    List<RimWorld.Apparel> apparels = pawn.apparel.WornApparel;
                    foreach (RimWorld.Apparel apparel in apparels) {
                        if (!apparel.def.apparel.tags.Contains("EVA")) continue;
                        if (apparel.def.apparel.layers.Contains(RimWorld.ApparelLayerDefOf.Overhead)) helmet = true;
                        if (apparel.def.apparel.layers.Contains(RimWorld.ApparelLayerDefOf.Shell) || apparel.def.apparel.layers.Contains(RimWorld.ApparelLayerDefOf.Middle)) suit = true;
                        if (helmet && suit) break;
                    }
                }
                // set protection value
                if (helmet && suit) {
                    value = Vacuum_Protection.All;
                } else if (helmet) {
                    value = Vacuum_Protection.Oxygen;
                }
            }
            pawn_protection_level.Add(pawn.thingIDNumber, value);
            return value;
        }

        public void remove(Map map) {
            if (map == null) {
                clear();
            } else {
                map_utilities.Remove(map.uniqueID);
                map_temperature.Remove(map.uniqueID);
            }
        }

        public void remove(Pawn pawn) {
            if (pawn == null) {
                clear();
            } else {
                pawn_protection_level.Remove(pawn.thingIDNumber);
            }
        }

        public void clear_utility_toggle() {
            utilities = new Dictionary<string, bool>();
        }

        public void clear() {
            utilities = new Dictionary<string, bool>();
            map_utilities = new Dictionary<int, Dictionary<string, bool>>();
            biome_utilities = new Dictionary<string, Dictionary<string, bool>>();
            terrain_utilities = new Dictionary<string, Dictionary<string, bool>>();
            gene_utilities = new Dictionary<string, Dictionary<string, bool>>();
            map_temperature = new Dictionary<int, float>();
            pawn_protection_level = new Dictionary<int, Vacuum_Protection>();
        }
    }

    /**
     * API for interacting with Caching_Handler class.
     */
    public static class Cache {
        public static Caching_Handler caching_handler;

        public static bool allowed_utility(string utility) => caching_handler.allowed_utility(utility);

        public static bool allowed_utility(Map map, string utility) => caching_handler.allowed_utility(map, utility);

        public static bool allowed_utility(RimWorld.BiomeDef biome, string utility) => caching_handler.allowed_utility(biome, utility);

        public static bool allowed_utility(TerrainDef terrain, string utility) => caching_handler.allowed_utility(terrain, utility);

        public static bool allowed_utility(GeneDef gene, string utility) => caching_handler.allowed_utility(gene, utility);

        public static float temperature(Map map) => caching_handler.temperature(map);

        public static Vacuum_Protection spacesuit_protection(Pawn pawn) => caching_handler.spacesuit_protection(pawn);

        public static void remove(Map map) => caching_handler.remove(map);

        public static void remove(Pawn pawn) => caching_handler.remove(pawn);

        public static void clear_utility_toggle() => caching_handler.clear_utility_toggle();

        public static void clear() => caching_handler.clear();
    }

    /**
     * Makes sure all cache is cleared when switching between worlds.
     */
    [HarmonyLib.HarmonyPatch(typeof(Game), "ClearCaches")]
    public static class Game_ClearCaches {
        public static void Postfix() => Cache.clear();
    }

    /**
     * Remove map from cache when map is deleted.
     */
    [HarmonyLib.HarmonyPatch(typeof(MapDeiniter), "Deinit_NewTemp")]
    public static class MapParent_Deinit_NewTemp {
        public static void Postfix(Map map) => Cache.remove(map);
    }

    /**
     * Remove pawn from cache if apparel is added.
     */
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.Pawn_ApparelTracker), "Notify_ApparelAdded")]
    public static class Pawn_ApparelTracker_Notify_ApparelAdded {
        public static void Postfix(RimWorld.Pawn_ApparelTracker __instance) => Cache.remove(__instance.pawn);
    }

    /**
     * Remove pawn from cache if apparel is removed.
     */
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.Pawn_ApparelTracker), "Notify_ApparelRemoved")]
    public static class Pawn_ApparelTracker_Notify_ApparelRemoved {
        public static void Postfix(RimWorld.Pawn_ApparelTracker __instance) => Cache.remove(__instance.pawn);
    }

    /**
     * Remove pawn from cache if dead.
     */
    [HarmonyLib.HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Pawn_Kill {
        public static void Postfix(Pawn __instance) => Cache.remove(__instance);
    }

    /**
     * Remove pawn from cache if genes are reimplanted.
     */
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.GeneUtility), "ReimplantXenogerm")]
    public static class GeneUtility_ReimplantXenogerm {
        public static void Postfix(Pawn caster, Pawn recipient) => Cache.remove(caster);
    }

    /**
     * Remove pawn from cache if genes are extracted.
     */
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.GeneUtility), "ExtractXenogerm")]
    public static class GeneUtility_ExtractXenogerm {
        public static void Postfix(Pawn pawn, int overrideDurationTicks = -1) => Cache.remove(pawn);
    }

    /**
     * Remove pawn from cache if genes are implanted.
     */
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.GeneUtility), "ImplantXenogermItem")]
    public static class GeneUtility_ImplantXenogermItem {
        public static void Postfix(Pawn pawn, RimWorld.Xenogerm xenogerm) => Cache.remove(pawn);
    }

    /**
     * Remove pawn from cache if genes are updated.
     */
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.GeneUtility), "UpdateXenogermReplication")]
    public static class GeneUtility_UpdateXenogermReplication {
        public static void Postfix(Pawn pawn) => Cache.remove(pawn);
    }
}
