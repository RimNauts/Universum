using System;
using UnityEngine;

namespace Universum.Defs {
    public class Component {
        public Type componentClass = typeof(World.ObjectComponent);

        public string materialDefName;

        public Vector3 offSet = Vector3.zero;
        public Color color = Color.white;
        public float hideAtMinAltitude = float.MaxValue;
        public float hideAtMaxAltitude = float.MinValue;

        public string overwriteText;
        public string fontPath;
        public float fontSize;
        public Color outlineColor = Color.black;
        public float outlineWidth;

        public float trailWidth;
        public float trailLength;
        public float trailTransparency;
    }
}
