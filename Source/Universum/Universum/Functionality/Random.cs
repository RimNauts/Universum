using System.Collections.Generic;
using UnityEngine;

namespace Universum.Functionality {
    public class Random {
        public int seed;
        private readonly System.Random _rand;

        public Random(int seed) {
            this.seed = seed;
            _rand = new System.Random(this.seed);
        }

        public bool GetBool() {
            return _rand.NextDouble() >= 0.5;
        }

        public float GetFloat() {
            return (float) _rand.NextDouble();
        }

        public float GetValueBetween(Vector2 range) {
            float min = range[0];
            float max = range[1];
            if (min == max) return min;
            if (min > max) {
                min = range[1];
                max = range[0];
            }
            return GetFloat() * (min - max) + min;
        }

        public int GetValueBetween(Vector2Int range) {
            int min = range[0];
            int max = range[1];
            if (min > max) {
                min = range[1];
                max = range[0];
            }
            max++;
            if (min == max) return min;
            return min + Mathf.Abs(_rand.Next() % (max - min));
        }

        public T GetElement<T>(List<T> array) {
            return array[GetValueBetween(new Vector2Int(0, array.Count - 1))];
        }
    }
}
