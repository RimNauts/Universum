using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Universum.Defs {
    public class CelestialObject : Def {
        public Type celestialObjectClass = typeof(World.CelestialObject);

        public string namePackDefName;

        public Vector2 scalePercentageBetween = Vector2.one;
        public Vector2 speedPercentageBetween = Vector2.one;
        public float orbitPathOffsetPercentage = 1.0f;
        public Vector2 orbitSpreadBetween = Vector2.one;
        public Vector2 orbitEccentricityBetween = Vector2.zero;
        public OrbitDirection orbitDirection = OrbitDirection.LEFT;
        public Vector2 axialRotationSpeedBetween = Vector2.one;

        public List<Component> components = new List<Component>();

        public Shape shape;
        public Icon icon;
    }
}
