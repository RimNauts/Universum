using UnityEngine;

namespace Universum.Defs {
    public class Noise {
        public bool isMask;
        public bool useMask;
        public Vector2 strenghtBetween = Vector2.one;
        public Vector2 roughnessBetween = Vector2.one;
        public Vector2 iterationsBetween = Vector2.one;
        public Vector2 persistenceBetween = Vector2.one;
        public Vector2 baseRoughnessBetween = Vector2.one;
        public Vector2 minNoiseValueBetween = Vector2.zero;
    }
}
