using System.Reflection;

namespace Universum {
    [Verse.StaticConstructorOnStartup]
    public static class Universum {
        static Universum() {
            new HarmonyLib.Harmony("sindre0830.universum").PatchAll(Assembly.GetExecutingAssembly());
            // print mod info
            Logger.print(
                Logger.Importance.Info,
                key: "Universum.Info.mod_loaded",
                args: new Verse.NamedArgument[] { Info.name, Info.version }
            );
            // load configuarations
            Settings.init();
            Utilities.Biome.Handler.init();
            Utilities.Terrain.Handler.init();
        }
    }
}
