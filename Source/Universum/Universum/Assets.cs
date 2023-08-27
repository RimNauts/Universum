using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Universum {
    [StaticConstructorOnStartup]
    public static class Assets {
        public static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        public static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static GameObject gameObjectWorldText;
        private static AssetBundle assets;

        public static void Init() {
            _GetAssets();

            shaders["Sprites/Default"] = Shader.Find("Sprites/Default");
            gameObjectWorldText = Resources.Load<GameObject>("Prefabs/WorldText");

            foreach (var (_, material) in Defs.Loader.materials) {
                MaterialRequest req = new MaterialRequest(GetShader(material.shaderName)) {
                    renderQueue = material.renderQueue,
                    color = material.color
                };
                if (material.texturePath != null) {
                    req.mainTex = GetTexture(material.texturePath);
                    req.needsMainTex = true;
                }
                materials[material.defName] = MaterialPool.MatFrom(req);
            }
        }

        public static Shader GetShader(string shaderName) {
            if (shaders.ContainsKey(shaderName)) return shaders[shaderName];
            shaders[shaderName] = _GetAsset(shaderName, ShaderDatabase.WorldOverlayCutout);
            return shaders[shaderName];
        }

        public static Texture2D GetTexture(string path) {
            if (textures.ContainsKey(path)) return textures[path];
            textures[path] = _GetContent<Texture2D>(path);
            return textures[path];
        }

        private static void _GetAssets() {
            string platformStr;
            switch (Application.platform) {
                case RuntimePlatform.OSXPlayer:
                    platformStr = "mac";
                    break;
                case RuntimePlatform.WindowsPlayer:
                    platformStr = "windows";
                    break;
                case RuntimePlatform.LinuxPlayer:
                    platformStr = "linux";
                    break;
                default:
                    Logger.print(
                        Logger.Importance.Info,
                        key: "RimNauts.Info.assets_loaded",
                        prefix: Style.tab,
                        args: new NamedArgument[] { "no supported" }
                    );
                    return;
            }
            foreach (var assets in Universum.ModContent.instance.Content.assetBundles.loadedAssetBundles) {
                if (assets.name.Contains(platformStr)) {
                    Assets.assets = assets;
                    Logger.print(
                        Logger.Importance.Info,
                        key: "RimNauts.Info.assets_loaded",
                        prefix: Style.tab,
                        args: new NamedArgument[] { platformStr }
                    );
                    return;
                }
            }
            Logger.print(
                Logger.Importance.Info,
                key: "RimNauts.Info.assets_loaded",
                prefix: Style.tab,
                args: new NamedArgument[] { "no supported" }
            );
        }

        private static T _GetAsset<T>(string name, T fallback = null) where T : Object {
            if (assets == null) return fallback;
            return assets.LoadAsset<T>(name);
        }

        public static T _GetContent<T>(string path, T fallback = null) where T : Object {
            if (assets == null) return fallback;
            return ContentFinder<T>.Get(path);
        }
    }
}
