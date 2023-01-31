using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Universum.Utilities {
    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L14
     */
    [HarmonyPatch(typeof(SkyManager), "SkyManagerUpdate")]
    public class SkyManager_SkyManagerUpdate {
        public static void Postfix() {
            if (!Cache.allowed_utility("Universum.remove_shadows")) return;
            if (!Cache.allowed_utility(Find.CurrentMap, "Universum.vacuum")) return;
            MatBases.LightOverlay.color = new Color(1.0f, 1.0f, 1.0f);
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/ShipInteriorMod2.cs#L2220
     */
    [HarmonyPatch(typeof(MapDrawer), "DrawMapMesh", null)]
    public static class MapDrawer_DrawMapMesh {
        public const float altitude = 1100f;

        public static void Prefix() {
            if (!Cache.allowed_utility("Universum.vacuum_overlay")) return;
            Map map = Find.CurrentMap;
            if (Globals.rendered || !Cache.allowed_utility(map, "Universum.vacuum")) return;

            RenderTexture oldTexture = Find.WorldCamera.targetTexture;
            RenderTexture oldSkyboxTexture = RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture;

            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.Planet;
            Find.WorldCameraDriver.JumpTo(Find.CurrentMap.Tile);
            Find.WorldCameraDriver.altitude = altitude;
            Find.WorldCameraDriver.GetType()
                .GetField("desiredAltitude", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(Find.WorldCameraDriver, altitude);

            float aspect = (float) UI.screenWidth / UI.screenHeight;

            Find.WorldCameraDriver.Update();
            Find.World.renderer.CheckActivateWorldCamera();
            Find.World.renderer.DrawWorldLayers();
            RimWorld.Planet.WorldRendererUtility.UpdateWorldShadersParams();

            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = Globals.render;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.aspect = aspect;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.Render();

            Find.WorldCamera.targetTexture = Globals.render;
            Find.WorldCamera.aspect = aspect;
            Find.WorldCamera.Render();

            RenderTexture.active = Globals.render;
            Globals.planet_screenshot.ReadPixels(new Rect(0, 0, 2048, 2048), 0, 0);
            Globals.planet_screenshot.Apply();
            RenderTexture.active = null;

            Find.WorldCamera.targetTexture = oldTexture;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = oldSkyboxTexture;
            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
            Find.World.renderer.CheckActivateWorldCamera();

            if (!((List<RimWorld.Planet.WorldLayer>) typeof(RimWorld.Planet.WorldRenderer).GetField("layers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Find.World.renderer)).FirstOrFallback().ShouldRegenerate) {
                Globals.rendered = true;
            }
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/ShipInteriorMod2.cs#L2283
     */
    [HarmonyPatch(typeof(SectionLayer), "FinalizeMesh", null)]
    public static class SectionLayer_FinalizeMesh {
        public static bool Prefix(SectionLayer __instance, Section ___section) {
            if (!Cache.allowed_utility("Universum.vacuum_overlay")) return true;
            if (__instance.GetType().Name != "SectionLayer_Terrain" || !Cache.allowed_utility(___section.map, "Universum.vacuum")) return true;
            bool foundSpace = false;
            foreach (IntVec3 cell in ___section.CellRect.Cells) {
                TerrainDef terrain1 = ___section.map.terrainGrid.TerrainAt(cell);
                if (Cache.allowed_utility(terrain1, "Universum.vacuum_overlay")) {
                    foundSpace = true;
                    Printer_Mesh.PrintMesh(__instance, Matrix4x4.TRS(cell.ToVector3() + new Vector3(0.5f, 0f, 0.5f), Quaternion.identity, Vector3.one), MeshMakerPlanes.NewPlaneMesh(1f), Globals.planet_mat);
                }
            }
            if (!foundSpace) {
                for (int i = 0; i < __instance.subMeshes.Count; i++) {
                    if (__instance.subMeshes[i].material == Globals.planet_mat) {
                        __instance.subMeshes.RemoveAt(i);
                    }
                }
            }
            return true;
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L110
     */
    [HarmonyPatch(typeof(RimWorld.MapInterface), "Notify_SwitchedMap")]
    public class MapInterface_Notify_SwitchedMap {
        public static bool MapIsSpace;

        public static void Postfix() {
            if (Find.CurrentMap == null || Scribe.mode != LoadSaveMode.Inactive) return;
            MapIsSpace = Cache.allowed_utility(Find.CurrentMap, "Universum.vacuum");
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L126
     */
    [HarmonyPatch(typeof(Game), "LoadGame")]
    public class Game_LoadGame {
        public static void Postfix() {
            Globals.rendered = false;
            MapInterface_Notify_SwitchedMap.Postfix();
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L153
     */
    [HarmonyPatch(typeof(Game), "UpdatePlay")]
    public class Game_UpdatePlay {
        public static CameraDriver Driver;
        public static Camera GameCamera;
        public static Vector3 Center;
        public static float CellsHigh;
        public static float CellsWide;
        public static Dictionary<Map, Dictionary<Section, SectionLayer>> MapSections = new Dictionary<Map, Dictionary<Section, SectionLayer>>();
        private static Vector3 lastCameraPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        public static void add_section(Map map, Section section, SectionLayer layer) {
            if (!MapSections.TryGetValue(map, out Dictionary<Section, SectionLayer> sections)) {
                sections = new Dictionary<Section, SectionLayer>();
                MapSections.Add(map, sections);
            }
            sections.Add(section, layer);
        }

        public static void Prefix() {
            if (!Cache.allowed_utility("Universum.vacuum_overlay")) return;
            if (!MapInterface_Notify_SwitchedMap.MapIsSpace || !MapSections.ContainsKey(Find.CurrentMap)) return;
            Center = GameCamera.transform.position;
            var ratio = (float) UI.screenWidth / UI.screenHeight;
            CellsHigh = UI.screenHeight / Find.CameraDriver.CellSizePixels;
            CellsWide = CellsHigh * ratio;
            if ((lastCameraPosition - Center).magnitude < 1e-4) return;
            lastCameraPosition = Center;
            var sections = MapSections[Find.CurrentMap];
            var visibleRect = Driver.CurrentViewRect;
            foreach (var entry in sections) {
                if (!visibleRect.Overlaps(entry.Key.CellRect)) continue;
                MeshRecalculateHelper.recalculate_layer(entry.Value);
            }
        }

        public static void Postfix() {
            if (!MeshRecalculateHelper.Tasks.Any()) return;
            Task.WaitAll(MeshRecalculateHelper.Tasks.ToArray());
            MeshRecalculateHelper.Tasks.Clear();
            foreach (var layer in MeshRecalculateHelper.LayersToDraw) {
                var mesh = layer.GetSubMesh(Globals.planet_mat);
                if (!mesh.finalized || mesh.disabled) continue;
                Graphics.DrawMesh(mesh.mesh, Vector3.zero, Quaternion.identity, mesh.material, 0);
            }
            MeshRecalculateHelper.LayersToDraw.Clear();
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L138
     */
    [HarmonyPatch(typeof(Game), "FinalizeInit")]
    public class Game_FinalizeInit {
        public static void Postfix() {
            Game_UpdatePlay.Driver = Find.CameraDriver;
            Game_UpdatePlay.GameCamera = Find.CameraDriver.GetComponent<Camera>();
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L69
     */
    public class MeshRecalculateHelper {
        public static List<Task> Tasks = new List<Task>();
        public static List<SectionLayer> LayersToDraw = new List<SectionLayer>();

        public static void recalculate_layer(SectionLayer instance) {
            var mesh = instance.GetSubMesh(Globals.planet_mat);
            Tasks.Add(Task.Factory.StartNew(() => recalculate_mesh(mesh)));
            LayersToDraw.Add(instance);
        }

        private static void recalculate_mesh(object info) {
            if (!(info is LayerSubMesh mesh)) {
                Logger.print(
                    Logger.Importance.Error,
                    key: "Universum.Error.thread_with_wrong_type",
                    prefix: Style.name_prefix
                );
                return;
            }
            lock (mesh) {
                mesh.finalized = false;
                mesh.Clear(MeshParts.UVs);
                for (var i = 0; i < mesh.verts.Count; i++) {
                    var xdiff = mesh.verts[i].x - Game_UpdatePlay.Center.x;
                    var xfromEdge = xdiff + Game_UpdatePlay.CellsWide / 2.0f;
                    var zdiff = mesh.verts[i].z - Game_UpdatePlay.Center.z;
                    var zfromEdge = zdiff + Game_UpdatePlay.CellsHigh / 2.0f;
                    mesh.uvs.Add(new Vector3(xfromEdge / Game_UpdatePlay.CellsWide, zfromEdge / Game_UpdatePlay.CellsHigh, 0.0f));
                }
                mesh.FinalizeMesh(MeshParts.UVs);
            }
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L27
     */
    [HarmonyPatch(typeof(Section), MethodType.Constructor, typeof(IntVec3), typeof(Map))]
    [StaticConstructorOnStartup]
    public class Section_Constructor {
        private static readonly Type SunShadowsType;
        private static readonly Type TerrainType;

        static Section_Constructor() {
            SunShadowsType = AccessTools.TypeByName("SectionLayer_SunShadows");
            TerrainType = AccessTools.TypeByName("SectionLayer_Terrain");
        }

        public static void Postfix(Map map, Section __instance, List<SectionLayer> ___layers) {
            if (!Cache.allowed_utility("Universum.vacuum_overlay")) return;
            if (!Cache.allowed_utility(map, "Universum.vacuum")) return;
            // Kill shadows
            if (Cache.allowed_utility("Universum.remove_shadows")) ___layers.RemoveAll(layer => SunShadowsType.IsInstanceOfType(layer));
            // Get and store terrain layer for recalculation
            var terrain = ___layers.Find(layer => TerrainType.IsInstanceOfType(layer));
            Game_UpdatePlay.add_section(map, __instance, terrain);
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/ecaf9bba7975524b61bb1d7f1a37655f5be35e20/Source/1.4/HideLightingLayersInSpace.cs#L56
     */
    public class SectionLayer_Terrain_Regenerate {
        public static void Postfix(SectionLayer __instance, Section ___section) {
            if (!Cache.allowed_utility("Universum.vacuum_overlay")) return;
            if (!Cache.allowed_utility(___section.map, "Universum.vacuum")) return;
            MeshRecalculateHelper.recalculate_layer(__instance);
        }
    }

    /**
     * Source: https://github.com/SonicTHI/SaveOurShip2Experimental/blob/main/Source/1.4/ShipInteriorMod2.cs#L4402
     */
    [HarmonyPatch(typeof(RimWorld.Scenario), "PostWorldGenerate")]
    public class Scenario_PostWorldGenerate {
        public static void Prefix() => Globals.rendered = false;
    }

    [HarmonyPatch(typeof(Game), "InitNewGame")]
    public class Game_GameInitData {
        public static void Prefix() => Globals.rendered = false;
    }
}
