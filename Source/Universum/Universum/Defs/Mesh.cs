using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Universum.Defs {
    public class Mesh : Def {
        public string materialDefName;
        public ShapeType type;
        public int subdivisionIterations;
        public int detail = 5;
        public float radius = 1.0f;
        public Vector3 dimensions = Vector3.one;
        public Color maxElevationColor;
        public Color minElevationColor;
        public List<Noise> noiseLayers = new List<Noise>();
    }
}
