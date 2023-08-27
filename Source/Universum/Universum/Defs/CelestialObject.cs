using System;
using UnityEngine;
using Verse;

namespace Universum.Defs {
    public class CelestialObject : Def {
        public Type celestialObjectClass = typeof(World.CelestialObject);

        public string namePackDefName;

        public Vector2 sizeBetween = Vector2.one;
        public Vector2 orbitSpeedBetween = Vector2.one;
        public Vector3 orbitPath = Vector3.one;
        public Vector3 orbitSpread = Vector3.one;
        public OrbitDirection orbitDirection = OrbitDirection.LEFT;
        public Vector2 axialRotationSpeedBetween = Vector2.one;

        public FloatingLabel floatingLabel;
        public Trail trail;

        public Shape shape;
        public Icon icon;
    }
}
