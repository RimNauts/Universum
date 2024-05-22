using System.Collections.Generic;
using Verse;

namespace Universum.World.Comps {
    public class GenerateObjectHolderMap_Properties : RimWorld.WorldObjectCompProperties {
        public string label;
        public string desc;

        public GenerateObjectHolderMap_Properties() => compClass = typeof(GenerateObjectHolderMap);
    }

    public class GenerateObjectHolderMap : RimWorld.Planet.WorldObjectComp {
        public GenerateObjectHolderMap_Properties Props => (GenerateObjectHolderMap_Properties) props;

        public override IEnumerable<Gizmo> GetGizmos() {
            ObjectHolder objectHolder = this.parent as ObjectHolder;
            if (DebugSettings.godMode && !objectHolder.HasMap && objectHolder.MapGeneratorDef != null) {
                yield return new Command_Action {
                    defaultLabel = Props.label,
                    defaultDesc = Props.desc,
                    action = GenerateMap,
                };
            }
        }

        public void GenerateMap() {
            ObjectHolder objectHolder = parent as ObjectHolder;
            objectHolder.CreateMap(RimWorld.Faction.OfPlayer, clearFog: true);
        }
    }
}
