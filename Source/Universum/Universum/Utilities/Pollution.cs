using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Universum.Utilities {
    /**
     * Stops pollution from being added to vacuum terrain.
     */
    [HarmonyPatch(typeof(PollutionGrid), "SetPolluted")]
    public static class PollutionGrid_SetPolluted {
        public static bool Prefix(IntVec3 cell, Map ___map) {
            if (Cache.allowed_utility(___map.terrainGrid.TerrainAt(cell), "universum.vacuum")) return false;
            return true;
        }
    }
}

