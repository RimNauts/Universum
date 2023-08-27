using System.Collections.Generic;
using Verse;

namespace Universum.Defs {
    [StaticConstructorOnStartup]
    public static class Loader {
        public static Dictionary<string, CelestialObject> celestialObjects = new Dictionary<string, CelestialObject>();
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Dictionary<string, NamePack> namePacks = new Dictionary<string, NamePack>();
        private static int _totalDefs;

        public static void Init() {
            foreach (CelestialObject celestialObject in DefDatabase<CelestialObject>.AllDefs) {
                celestialObjects[celestialObject.defName] = celestialObject;
                _totalDefs++;
            }
            foreach (Material material in DefDatabase<Material>.AllDefs) {
                materials[material.defName] = material;
                _totalDefs++;
            }
            foreach (NamePack namePack in DefDatabase<NamePack>.AllDefs) {
                namePacks[namePack.defName] = namePack;
                _totalDefs++;
            }
            // print mod info
            Logger.print(
                Logger.Importance.Info,
                key: "RimNauts.Info.def_loader_done",
                prefix: Style.tab,
                args: new NamedArgument[] { _totalDefs }
            );
        }
    }
}
