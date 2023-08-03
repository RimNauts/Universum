using UnityEngine;
using Verse;

namespace Universum {
    [StaticConstructorOnStartup]
    public static class Globals {
        public static Texture2D planet_screenshot = new Texture2D(2048, 2048, TextureFormat.RGB24, false);
        public static RenderTexture render = new RenderTexture(2048, 2048, 16);
        public static Material planet_mat = MaterialPool.MatFrom(planet_screenshot);
        public static Material planet_mat_glass = new Material(planet_mat);
        public static bool rendered = false;
        public static float planet_render_altitude = 1100f;

        static Globals() {
            planet_mat_glass.color = Color.gray;
        }
    }
}
