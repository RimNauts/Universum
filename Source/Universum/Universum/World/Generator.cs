using System;

namespace Universum.World {
    public static class Generator {
        public static CelestialObject Create(Defs.CelestialObject celestialObjectDef) {
            CelestialObject celestialObject = (CelestialObject) Activator.CreateInstance(
                celestialObjectDef.celestialObjectClass,
                new object[] { celestialObjectDef.defName }
            );

            celestialObject.Init();

            return celestialObject;
        }
    }
}
