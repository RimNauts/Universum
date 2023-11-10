using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Universum.World.Patch {
    public class SettleInEmptyTileUtility {
        private static Dictionary<Defs.CelestialObject, Command> _commands = new Dictionary<Defs.CelestialObject, Command>();

        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(SettleInEmptyTileUtility_Settle)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(SettleInEmptyTileUtility_SettleCommand)).Patch();
        }

        public static Command getCommand(ObjectHolder objectHolder, RimWorld.Planet.Caravan caravan) {
            Defs.CelestialObject celestialObjectDef = objectHolder.celestialObjectDef;

            if (_commands.TryGetValue(celestialObjectDef, out var command)) return command;

            Command_Settle newCommand = new Command_Settle {
                defaultLabel = (string) celestialObjectDef.objectHolder.commandLabelKey.Translate(),
                defaultDesc = (string) celestialObjectDef.objectHolder.commandDescKey.Translate(),
                icon = Assets.GetTexture(celestialObjectDef.objectHolder.commandIconPath),
                action = () => RimWorld.Planet.SettleInEmptyTileUtility.Settle(caravan)
            };

            _commands[celestialObjectDef] = newCommand;
            return newCommand;
        }
    }

    [HarmonyPatch]
    static class SettleInEmptyTileUtility_Settle {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.SettleInEmptyTileUtility:Settle");

        public static bool Prefix(RimWorld.Planet.Caravan caravan) {
            ObjectHolder objectHolder = ObjectHolderCache.Get(caravan.Tile);
            if (objectHolder == null) return true;

            objectHolder.Settle(caravan);

            return false;
        }
    }

    [HarmonyPatch]
    static class SettleInEmptyTileUtility_SettleCommand {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.SettleInEmptyTileUtility:SettleCommand");

        public static bool Prefix(RimWorld.Planet.Caravan caravan, ref Command __result) {
            ObjectHolder objectHolder = ObjectHolderCache.Get(caravan.Tile);
            if (objectHolder == null) return true;

            __result = SettleInEmptyTileUtility.getCommand(objectHolder, caravan);

            return false;
        }
    }
}
