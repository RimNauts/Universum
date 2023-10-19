using RimWorld;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Verse;

namespace Universum.World {
    public class Generator : WorldGenStep {
        public override int SeedPart => 0;

        public override void GenerateFresh(string seed) {
            List<string> celestialObjectDefNames = new List<string>();
            foreach (Defs.ObjectGeneration objectGenerationStep in Defs.Loader.celestialObjectGenerationStartUpSteps.Values) {
                for (int i = 0; i < objectGenerationStep.total; i++) {
                    celestialObjectDefNames.Add(objectGenerationStep.objectGroup.RandomElementByWeight(o => o.tickets).celestialDefName);
                }
            }
            Create(celestialObjectDefNames);
        }

        public static List<CelestialObject> Generate(Defs.ObjectGeneration objectGenerationStep, Vector2 despawnBetweenDays, int? amount = null) {
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
            return Create(celestialObjectDefNames, deathTicks: celestialObjectDeathTicks);
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

            Thread thread = new Thread(new ParameterizedThreadStart(CreateVisuals));
            thread.Start(celestialObjects);

            return celestialObjects;
        }

        public static CelestialObject Create(string celestialObjectDefName, int? seed = null, Vector3? position = null) {
            CelestialObject celestialObject = (CelestialObject) Activator.CreateInstance(
                Defs.Loader.celestialObjects[celestialObjectDefName].celestialObjectClass,
                new object[] { celestialObjectDefName }
            );

            celestialObject.Init(seed, position);

            Game.MainLoop.instance.AddObject(celestialObject);

            celestialObject.GenerateVisuals();

            return celestialObject;
        }

        private static void CreateVisuals(object celestialObjects) {
            int seed = Game.MainLoop.instance.seed;
            List<CelestialObject> newCelestialObjects = (List<CelestialObject>) celestialObjects;

            foreach (var celestialObject in newCelestialObjects) {
                if (Game.MainLoop.instance.seed != seed) return;
                celestialObject.GenerateVisuals();
            }
        }
    }
}
