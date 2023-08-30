using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Verse;

namespace Universum.World {
    public static class Generator {
        public static List<CelestialObject> Create(List<string> celestialObjectDefNames, List<int?> seeds = null, List<Vector3?> positions = null) {
            List<CelestialObject> celestialObjects = new List<CelestialObject>();

            for (int i = 0; i < celestialObjectDefNames.Count; i++) {
                string celestialObjectDefName = celestialObjectDefNames[i];

                int? seed = null;
                if (!seeds.NullOrEmpty()) seed = seeds[i];

                Vector3? position = null;
                if (!seeds.NullOrEmpty()) position = positions[i];

                CelestialObject celestialObject = (CelestialObject) Activator.CreateInstance(
                    Defs.Loader.celestialObjects[celestialObjectDefName].celestialObjectClass,
                    new object[] { celestialObjectDefName }
                );

                celestialObject.Init(seed, position);
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
            List<CelestialObject> newCelestialObjects = (List<CelestialObject>) celestialObjects;

            foreach (var celestialObject in newCelestialObjects) {
                if (Scribe.mode == LoadSaveMode.LoadingVars) return;
                celestialObject.GenerateVisuals();
                Game.MainLoop.instance.dirtyCache = true;
            }
        }
    }
}
