using System.Reflection;

namespace Universum {
    [Verse.StaticConstructorOnStartup]
    public static class Universum {
        static Universum() {
            // apply patch on internal class
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("sindre0830.universum");
            harmony.Patch(
                original: HarmonyLib.AccessTools.TypeByName("SectionLayer_Terrain").GetMethod("Regenerate"),
                postfix: new HarmonyLib.HarmonyMethod(typeof(Utilities.SectionLayer_Terrain_Regenerate).GetMethod("Postfix"))
            );
            harmony.PatchAll(Assembly.GetExecutingAssembly());
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
            Utilities.Stat.Handler.init();
        }
    }
}
