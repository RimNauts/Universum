using System.Reflection;
using Verse;
using UnityEngine;

namespace Universum {
    [StaticConstructorOnStartup]
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
                args: new NamedArgument[] { Info.name, ModContent.instance.Content.ModMetaData.ModVersion }
            );
            // load configuarations
            Defs.Loader.Init();
            Settings.init();
            Utilities.Biome.Handler.init();
            Utilities.Terrain.Handler.init();
            if (ModsConfig.BiotechActive) Utilities.Gene.Handler.init();
            Assets.Init();
            // branch if camera+ patch needs to be applied
            if (ModsConfig.IsActive("brrainz.cameraplus")) {
                Globals.planet_mat.mainTextureOffset = new Vector2(0.3f, 0.3f);
                Globals.planet_mat.mainTextureScale = new Vector2(0.4f, 0.4f);
                Globals.planet_mat_glass.mainTextureOffset = new Vector2(0.3f, 0.3f);
                Globals.planet_mat_glass.mainTextureScale = new Vector2(0.4f, 0.4f);
                Globals.planet_render_altitude *= 1.6f;
                Logger.print(
                    Logger.Importance.Info,
                    key: "Universum.Info.camera_patch_applied",
                    prefix: Style.tab
                );
            }
        }

        public class ModContent : Mod {
            public static ModContent instance { get; private set; }

            public ModContent(ModContentPack content) : base(content) {
                instance = this;
            }
        }
    }
}
