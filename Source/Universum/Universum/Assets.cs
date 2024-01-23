using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Universum {
    [StaticConstructorOnStartup]
    public static class Assets {
        private static AssetBundle assets;
        public static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        public static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static GameObject gameObjectWorldText;
        public static RimWorld.WorldObjectDef objectHolderDef;
        public static RimWorld.BiomeDef oceanBiomeDef;
        public static Quaternion billboardCorrectionRotation = Quaternion.Euler(90, 90, 0);

        public static void Init() {
            _GetAssets();

            shaders["Sprites/Default"] = Shader.Find("Sprites/Default");
            gameObjectWorldText = Resources.Load<GameObject>("Prefabs/WorldText");
            objectHolderDef = DefDatabase<RimWorld.WorldObjectDef>.GetNamed("Universum_ObjectHolder");
            oceanBiomeDef = DefDatabase<RimWorld.BiomeDef>.GetNamed("Ocean");

            foreach (var (_, materialDef) in Defs.Loader.materials) {
                Shader shaderInstance = GetShader(materialDef.shaderName);
                Material material = new Material(shaderInstance);

                material.renderQueue = materialDef.renderQueue;
                material.color = materialDef.color;

                if (materialDef.texturePath != null) material.mainTexture = GetTexture(materialDef.texturePath);

                foreach (Defs.ShaderProperties properties in materialDef.shaderProperties) {
                    if (properties.floatValue != null) {
                        material.SetFloat(properties.name, (float) properties.floatValue);
                    } else if (properties.colorValue != null) {
                        material.SetColor(properties.name, (Color) properties.colorValue);
                    } else if (properties.texturePathValue != null) {
                        material.SetTexture(properties.name, GetTexture(properties.texturePathValue));
                    }
                }

                materials[materialDef.defName] = material;
            }

            foreach (var (_, celestialObjectDef) in Defs.Loader.celestialObjects) {
                if (celestialObjectDef.icon != null) GetTexture(celestialObjectDef.icon.texturePath);
                if (celestialObjectDef.objectHolder == null) continue;
                if (celestialObjectDef.objectHolder.overlayIconPath != null) GetTexture(celestialObjectDef.objectHolder.overlayIconPath);
                if (celestialObjectDef.objectHolder.commandIconPath != null) GetTexture(celestialObjectDef.objectHolder.commandIconPath);
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
