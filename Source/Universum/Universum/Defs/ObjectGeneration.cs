using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace Universum.Defs {
    public class ObjectGeneration : Def {
        public int total;
        public InitializationType initializationType = InitializationType.START_UP;
        public Vector2 spawnBetweenDays = Vector2.one;
        public Vector2 despawnBetweenDays = Vector2.zero;
        public Vector2 spawnAmountBetween = Vector2.one;
        public List<ObjectGenerationChance> objectGroup = new List<ObjectGenerationChance>();
    }
}
