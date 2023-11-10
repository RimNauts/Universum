using Verse;

namespace Universum.Defs {
    public class ObjectHolder {
        public MapGeneratorDef mapGeneratorDef;
        public RimWorld.BiomeDef biomeDef;
        public bool keepAfterAbandon;
        public string description = "";
        public string overlayIconPath = "Universum_Transparent";
        public string commandLabelKey = "CommandSettle";
        public string commandDescKey = "CommandSettleDesc";
        public string commandIconPath = "UI/Commands/Settle";
    }
}
