using System.Collections.Generic;
using Verse;

namespace Universum.Defs {
    public class ObjectGeneration : Def {
        public int total;
        public List<ObjectGenerationChance> objectGroup = new List<ObjectGenerationChance>();
    }
}
