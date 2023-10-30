using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Universum.Defs {
    public class Material : Def {
        public string shaderName;
        public string texturePath;
        public Color color = Color.white;
        public int renderQueue = RimWorld.Planet.WorldMaterials.WorldObjectRenderQueue;
        public string bumpMap;
        public List<ShaderProperties> shaderProperties = new List<ShaderProperties>();
    }
}
