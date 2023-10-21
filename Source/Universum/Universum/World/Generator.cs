using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Universum.World {
    public class Generator : WorldGenStep {
        public override int SeedPart => 0;

        public override void GenerateFresh(string seed) {
            List<string> celestialObjectDefNames = new List<string>();
            List<string> objectHolderDefNames = new List<string>();
            foreach (Defs.ObjectGeneration objectGenerationStep in Defs.Loader.celestialObjectGenerationStartUpSteps.Values) {
                for (int i = 0; i < objectGenerationStep.total; i++) {
                    string celestialDefName = objectGenerationStep.objectGroup.RandomElementByWeight(o => o.tickets).celestialDefName;
                    if (Defs.Loader.celestialObjects[celestialDefName].objectHolder != null) {
                        objectHolderDefNames.Add(celestialDefName);
                    } else celestialObjectDefNames.Add(celestialDefName);
                }
            }
            foreach (var objectHolderDefName in objectHolderDefNames) CreateObjectHolder(objectHolderDefName);
            Create(celestialObjectDefNames);
        }

        public static void Generate(Defs.ObjectGeneration objectGenerationStep, Vector2 despawnBetweenDays, int? amount = null) {
            int total = amount ?? objectGenerationStep.total;
            List<string> celestialObjectDefNames = new List<string>();
            List<int?> celestialObjectDeathTicks = new List<int?>();
            for (int i = 0; i < total; i++) {
                celestialObjectDefNames.Add(objectGenerationStep.objectGroup.RandomElementByWeight(o => o.tickets).celestialDefName);
                if (despawnBetweenDays != Vector2.zero) {
                    int deathTick = (int) Rand.Range(despawnBetweenDays[0] * 60000, despawnBetweenDays[1] * 60000) + Game.MainLoop.tickManager.TicksGame;
                    celestialObjectDeathTicks.Add(deathTick);
                } else celestialObjectDeathTicks.Add(null);
            }
            Create(celestialObjectDefNames, deathTicks: celestialObjectDeathTicks);
        }

        public static List<CelestialObject> Create(
            List<string> celestialObjectDefNames,
            List<int?> seeds = null,
            List<Vector3?> positions = null,
            List<int?> deathTicks = null
        ) {
            List<CelestialObject> celestialObjects = new List<CelestialObject>();
            for (int i = 0; i < celestialObjectDefNames.Count; i++) {
                string celestialObjectDefName = celestialObjectDefNames[i];
                
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
            CelestialObject celestialObject = null
        ) {
            ObjectHolder objectHolder = (ObjectHolder) Activator.CreateInstance(Assets.objectHolderDef.worldObjectClass);
            objectHolder.def = Assets.objectHolderDef;
            objectHolder.ID = Find.UniqueIDsManager.GetNextWorldObjectID();
            objectHolder.creationGameTicks = Find.TickManager.TicksGame;
            objectHolder.Tile = GetFreeTile();
            if (objectHolder.Tile == -1) {
                objectHolder.Destroy();
                return null;
            }
            objectHolder.Init(celestialObjectDefName, celestialObjectSeed, celestialObjectPosition, celestialObjectDeathTick, celestialObject);
            objectHolder.PostMake();
            Find.WorldObjects.Add(objectHolder);
            return objectHolder;
        }

        public static void TileClear(int tile) {
            Find.World.grid.tiles.ElementAt(tile).biome = Assets.oceanBiomeDef;
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
