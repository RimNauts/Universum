using System.Collections.Generic;

namespace Universum.Utilities {
    /**
     * Keeps all the cache collections and their methods.
     */
    public class Caching_Handler : Verse.GameComponent {
        public Dictionary<string, bool> utilities;
        public Dictionary<int, Dictionary<string, bool>> map_utilities;
        public Dictionary<string, Dictionary<string, bool>> biome_utilities;
        public Dictionary<string, Dictionary<string, bool>> terrain_utilities;
        public Dictionary<int, float> map_temperature;
        public Dictionary<int, Vacuum_Protection> pawn_protection_level;

        public Caching_Handler(Verse.Game game) : base() {
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

        public bool allowed_utility(Verse.Map map, string utility) {
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

        public bool allowed_utility(Verse.TerrainDef terrain, string utility) {
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

        public float temperature(Verse.Map map) {
            if (map == null) return 0.0f;
            // branch if map is cached
            if (map_temperature.TryGetValue(map.uniqueID, out var temp)) return temp;
            // get value and cache result
            var property_value = Biome.Handler.get_properties(map.Biome).temperature;
            map_temperature.Add(map.uniqueID, property_value);
            return property_value;
        }

        public Vacuum_Protection spacesuit_protection(Verse.Pawn pawn) {
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
                foreach (RimWorld.Apparel apparel in pawn.apparel.WornApparel) {
                    if (apparel.def.apparel.tags.Contains("EVA")) {
                        if (apparel.def.apparel.layers.Contains(RimWorld.ApparelLayerDefOf.Overhead)) helmet = true;
                        if (apparel.def.apparel.layers.Contains(RimWorld.ApparelLayerDefOf.Shell) || apparel.def.apparel.layers.Contains(RimWorld.ApparelLayerDefOf.Middle)) suit = true;
                        if (helmet && suit) break;
                    }
                }
                if (helmet && suit) {
                    value = Vacuum_Protection.All;
                } else if (helmet) {
                    value = Vacuum_Protection.Oxygen;
                }
            }
            pawn_protection_level.Add(pawn.thingIDNumber, value);
            return value;
        }

        public void remove(Verse.Map map) {
            if (map == null) {
                clear();
            } else {
                map_utilities.Remove(map.uniqueID);
                map_temperature.Remove(map.uniqueID);
            }
        }

        public void remove(Verse.Pawn pawn) {
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

        public static bool allowed_utility(Verse.Map map, string utility) => caching_handler.allowed_utility(map, utility);

        public static bool allowed_utility(RimWorld.BiomeDef biome, string utility) => caching_handler.allowed_utility(biome, utility);

        public static bool allowed_utility(Verse.TerrainDef terrain, string utility) => caching_handler.allowed_utility(terrain, utility);

        public static float temperature(Verse.Map map) => caching_handler.temperature(map);

        public static Vacuum_Protection spacesuit_protection(Verse.Pawn pawn) => caching_handler.spacesuit_protection(pawn);

        public static void remove(Verse.Map map) => caching_handler.remove(map);

        public static void remove(Verse.Pawn pawn) => caching_handler.remove(pawn);

        public static void clear_utility_toggle() => caching_handler.clear_utility_toggle();

        public static void clear() => caching_handler.clear();
    }

    /**
     * Makes sure all cache is cleared when switching between worlds.
     */
    [HarmonyLib.HarmonyPatch(typeof(Verse.Game), "ClearCaches")]
    public static class Game_ClearCaches {
        public static void Postfix() => Cache.clear();
    }

    /**
     * Remove map from cache when map is deleted.
     */
    [HarmonyLib.HarmonyPatch(typeof(Verse.MapDeiniter), "Deinit_NewTemp")]
    public static class MapParent_Deinit_NewTemp {
        public static void Postfix(Verse.Map map) => Cache.remove(map);
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
    [HarmonyLib.HarmonyPatch(typeof(Verse.Pawn), "Kill")]
    public static class Pawn_Kill {
        public static void Postfix(Verse.Pawn __instance) => Cache.remove(__instance);
    }
}
