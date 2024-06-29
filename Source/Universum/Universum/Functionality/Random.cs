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

        public Quaternion GetRotation() => Quaternion.Euler(GetFloat() * 360, GetFloat() * 360, GetFloat() * 360);

        public bool GetBool() {
            return _rand.NextDouble() >= 0.5;
        }

        public float GetFloat() {
            return (float) _rand.NextDouble();
        }

        public float GetValueBetween(Vector2 range) {
            float min = range.x;
            float max = range.y;

            if (min == max) return min;

            if (min > max) {
                float tmp = min;

                min = max;
                max = tmp;
            }

            return GetFloat() * (max - min) + min;
        }

        public int GetValueBetween(Vector2Int range) {
            int min = range.x;
            int max = range.y;

            if (min == max) return min;

            if (min > max) {
                int tmp = min;

                min = max;
                max = tmp;
            }

            return Mathf.Abs(_rand.Next(min, max + 1));
        }

        public T GetElement<T>(List<T> array) {
            return array[GetValueBetween(new Vector2Int(0, array.Count - 1))];
        }
    }
}
