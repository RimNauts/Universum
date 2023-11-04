using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Universum.World {
    public class Generator : WorldGenStep {
        public override int SeedPart => 0;

        public override void GenerateFresh(string seed) {
            Game.MainLoop.instance.FreshGame();
            GenerateOnStartUp();
        }

        public static void GenerateOnStartUp() {
            foreach (Defs.ObjectGeneration objectGenerationStep in Defs.Loader.celestialObjectGenerationStartUpSteps.Values) {
                int total = Settings.totalToSpawnGenStep[objectGenerationStep.defName];
                for (int i = 0; i < total; i++) {
                    string celestialDefName = objectGenerationStep.objectGroup.RandomElementByWeight(o => o.tickets).celestialDefName;
                    if (Defs.Loader.celestialObjects[celestialDefName].objectHolder != null) {
                        CreateObjectHolder(celestialDefName);
                    } else Create(celestialDefName);
                }
            }
        }

        public static void Regenerate() {
            foreach (Defs.ObjectGeneration objectGenerationStep in Defs.Loader.celestialObjectGenerationStartUpSteps.Values) {
                bool respawnFlag = true;
                foreach (var objectToSpawn in objectGenerationStep.objectGroup) {
                    if (Defs.Loader.celestialObjects[objectToSpawn.celestialDefName].objectHolder != null) respawnFlag = false;
                }
                if (!respawnFlag) continue;

                foreach (var objectToSpawn in objectGenerationStep.objectGroup) Game.MainLoop.instance.ShouldDestroy(Defs.Loader.celestialObjects[objectToSpawn.celestialDefName]);
                Game.MainLoop.instance.GameComponentUpdate();

                int total = Settings.totalToSpawnGenStep[objectGenerationStep.defName];
                for (int i = 0; i < total; i++) {
                    string celestialDefName = objectGenerationStep.objectGroup.RandomElementByWeight(o => o.tickets).celestialDefName;
                    if (Defs.Loader.celestialObjects[celestialDefName].objectHolder != null) {
                        CreateObjectHolder(celestialDefName);
                    } else Create(celestialDefName);
                }
            }
            Game.MainLoop.instance.dirtyCache = true;
        }

        public static void Generate(Defs.ObjectGeneration objectGenerationStep, Vector2 despawnBetweenDays, int? amount = null) {
            int totalObjectsAlive = 0;
            foreach (var objectToSpawn in objectGenerationStep.objectGroup) totalObjectsAlive += Game.MainLoop.instance.GetTotal(Defs.Loader.celestialObjects[objectToSpawn.celestialDefName]);
            if (totalObjectsAlive >= Settings.totalToSpawnGenStep[objectGenerationStep.defName]) return;

            int total = amount ?? Settings.totalToSpawnGenStep[objectGenerationStep.defName];
            List<string> celestialDefNames = new List<string>();
            List<ObjectHolder> objectHolders = new List<ObjectHolder>();

            for (int i = 0; i < total; i++) {
                string celestialDefName = objectGenerationStep.objectGroup.RandomElementByWeight(o => o.tickets).celestialDefName;
                celestialDefNames.Add(celestialDefName);

                int? deathTick = null;
                if (despawnBetweenDays != Vector2.zero) deathTick = (int) Rand.Range(despawnBetweenDays[0] * 60000, despawnBetweenDays[1] * 60000) + Game.MainLoop.instance.tick;

                if (Defs.Loader.celestialObjects[celestialDefName].objectHolder != null) {
                    ObjectHolder objectHolder = CreateObjectHolder(celestialDefName, celestialObjectDeathTick: deathTick);
                    objectHolders.Add(objectHolder);
                } else Create(celestialDefName, deathTick: deathTick);
            }

            SendLetter(objectGenerationStep, celestialDefNames, objectHolders);
        }

        public static void SendLetter(Defs.ObjectGeneration objectGenerationStep, List<string> celestialDefNames, List<ObjectHolder> objectHolders) { }

        public static List<CelestialObject> Create(
            List<string> celestialObjectDefNames,
            List<int?> seeds = null,
            List<Vector3?> positions = null,
            List<int?> deathTicks = null
        ) {
            List<CelestialObject> celestialObjects = new List<CelestialObject>();
            for (int i = 0; i < celestialObjectDefNames.Count; i++) {
                string celestialObjectDefName = celestialObjectDefNames[i];

                if (!Defs.Loader.celestialObjects.ContainsKey(celestialObjectDefName)) continue;
                
                int? seed = null;
                if (!seeds.NullOrEmpty()) seed = seeds[i];
                
                Vector3? position = null;
                if (!seeds.NullOrEmpty()) position = positions[i];

                int? deathTick = null;
                if (!deathTicks.NullOrEmpty()) deathTick = deathTicks[i];

                CelestialObject celestialObject = (CelestialObject) Activator.CreateInstance(
                    Defs.Loader.celestialObjects[celestialObjectDefName].celestialObjectClass,
                    new object[] { celestialObjectDefName }
                );
                
                celestialObject.Init(seed, position, deathTick);
                celestialObjects.Add(celestialObject);
            }
            Game.MainLoop.instance.AddObject(celestialObjects);

            return celestialObjects;
        }

        public static CelestialObject Create(string celestialObjectDefName, int? seed = null, Vector3? position = null, int? deathTick = null) {
            CelestialObject celestialObject = (CelestialObject) Activator.CreateInstance(
                Defs.Loader.celestialObjects[celestialObjectDefName].celestialObjectClass,
                new object[] { celestialObjectDefName }
            );

            celestialObject.Init(seed, position, deathTick);

            Game.MainLoop.instance.AddObject(celestialObject);

            return celestialObject;
        }

        public static ObjectHolder CreateObjectHolder(
            string celestialObjectDefName,
            int? celestialObjectSeed = null,
            Vector3? celestialObjectPosition = null,
            int? celestialObjectDeathTick = null,
            CelestialObject celestialObject = null,
            int tile = -1
        ) {
            if (tile == -1) tile = GetFreeTile();
            if (tile == -1) return null;
            UpdateTile(tile, Defs.Loader.celestialObjects[celestialObjectDefName].objectHolder.biomeDef);

            ObjectHolder objectHolder = (ObjectHolder) Activator.CreateInstance(Assets.objectHolderDef.worldObjectClass);
            objectHolder.def = Assets.objectHolderDef;
            objectHolder.ID = Find.UniqueIDsManager.GetNextWorldObjectID();
            objectHolder.creationGameTicks = Find.TickManager.TicksGame;
            objectHolder.Tile = tile;

            objectHolder.Init(celestialObjectDefName, celestialObjectSeed, celestialObjectPosition, celestialObjectDeathTick, celestialObject);
            objectHolder.PostMake();
            Find.WorldObjects.Add(objectHolder);

            return objectHolder;
        }

        public static void UpdateTile(int tile, RimWorld.BiomeDef biome) {
            Find.World.grid.tiles.ElementAt(tile).biome = biome;
            Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(tile);
        }

        public static int GetFreeTile(int startIndex = 1) {
            for (int i = startIndex; i < Find.World.grid.TilesCount; i++) {
                if (Find.World.grid.tiles.ElementAt(i).biome == Assets.oceanBiomeDef && !Find.World.worldObjects.AnyWorldObjectAt(i)) {
                    List<int> neighbors = new List<int>();
                    Find.World.grid.GetTileNeighbors(i, neighbors);
                    if (neighbors.Count != 6) continue;

                    var flag = false;
                    foreach (var neighbour in neighbors) {
                        var neighbourTile = Find.World.grid.tiles.ElementAtOrDefault(neighbour);
                        if (neighbourTile != default(RimWorld.Planet.Tile)) {
                            if (neighbourTile.biome != Assets.oceanBiomeDef) {
                                flag = true;
                                break;
                            }
                        }
                    }

                    if (flag) continue;

                    return i;
                }
            }
            return -1;
        }
    }
}
