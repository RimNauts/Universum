using System.Collections.Generic;
using UnityEngine;

namespace Universum.Defs {
    public class Mesh {
        public string materialDefName;
        public float radius = 1.0f;
        public Color maxElevationColor;
        public Color minElevationColor;
        public List<Noise> noiseLayers = new List<Noise>();
    }
}
