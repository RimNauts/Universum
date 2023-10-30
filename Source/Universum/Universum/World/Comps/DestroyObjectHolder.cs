using System.Collections.Generic;
using Verse;

namespace Universum.World.Comps {
    public class DestroyObjectHolder_Properties : RimWorld.WorldObjectCompProperties {
        public string label;
        public string desc;

        public DestroyObjectHolder_Properties() => compClass = typeof(DestroyObjectHolder);
    }

    public class DestroyObjectHolder : RimWorld.Planet.WorldObjectComp {
        public DestroyObjectHolder_Properties Props => (DestroyObjectHolder_Properties) props;

        public override IEnumerable<Gizmo> GetGizmos() {
            ObjectHolder objectHolder = parent as ObjectHolder;
            if (DebugSettings.godMode && !objectHolder.HasMap) {
                yield return new Command_Action {
                    defaultLabel = Props.label,
                    defaultDesc = Props.desc,
                    action = DestroyObject,
                };
            }
        }

        public void DestroyObject() {
            ObjectHolder objectHolder = parent as ObjectHolder;
            objectHolder.SignalDestruction();
        }
    }
}
