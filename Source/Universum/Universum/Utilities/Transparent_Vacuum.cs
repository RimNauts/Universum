using UnityEngine;

namespace Universum.Utilities {
    // https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L14
    [HarmonyLib.HarmonyPatch(typeof(Verse.SkyManager), "SkyManagerUpdate")]
    public class SkyManager_SkyManagerUpdate {
        public static void Postfix() {
            if (Cache.allowed_utility(Verse.Find.CurrentMap, "Universum.vacuum")) return;
            Verse.MatBases.LightOverlay.color = new Color(1.0f, 1.0f, 1.0f);
        }
    }
}
