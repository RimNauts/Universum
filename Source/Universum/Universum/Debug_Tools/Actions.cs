using Verse;

namespace Universum.Debug_Tools {
    public static class DebugActions {
        [DebugAction("Universum", "Reset SkyManager", false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ResetSkyManager() {
            Find.CurrentMap.skyManager.SkyManagerUpdate();
        }
    }
}
