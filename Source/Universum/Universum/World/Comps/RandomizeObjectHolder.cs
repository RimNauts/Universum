using System.Collections.Generic;
using Verse;

namespace Universum.World.Comps {
    public class RandomizeObjectHolder_Properties : RimWorld.WorldObjectCompProperties {
        public string label;
        public string desc;

        public RandomizeObjectHolder_Properties() => compClass = typeof(RandomizeObjectHolder);
    }

    public class RandomizeObjectHolder : RimWorld.Planet.WorldObjectComp {
        public RandomizeObjectHolder_Properties Props => (RandomizeObjectHolder_Properties) props;

        public override IEnumerable<Gizmo> GetGizmos() {
            if (DebugSettings.godMode) {
                yield return new Command_Action {
                    defaultLabel = Props.label,
                    defaultDesc = Props.desc,
                    action = RandomizeObject,
                };
            }
        }

        public void RandomizeObject() {
            ObjectHolder objectHolder = this.parent as ObjectHolder;
            objectHolder.Randomize();
        }
    }
}
