using System.Collections.Generic;
using Verse;

namespace Universum.Utilities.Stat {
    public class Properties : Verse.DefModExtension {
        public List<string> allowed_utilities = new List<string>();
        private static readonly Dictionary<string, Properties> DefaultsByPackageId = new Dictionary<string, Properties>();
        private static readonly Dictionary<string, Properties> DefaultsByDefName = new Dictionary<string, Properties>();

        public static Properties[] GetAll() {
            Properties[] statPropertiesArray = new Properties[Verse.DefDatabase<RimWorld.StatDef>.DefCount];
            foreach (RimWorld.StatDef stat in Verse.DefDatabase<RimWorld.StatDef>.AllDefs) {
                Properties statProperties = ((stat.GetModExtension<Properties>() ?? DefaultsByDefName.TryGetValue(stat.defName)) ?? DefaultsByPackageId.TryGetValue(stat.modContentPack.ModMetaData.PackageIdNonUnique)) ?? new Properties();
                statPropertiesArray[stat.index] = statProperties;
            }
            return statPropertiesArray;
        }
    }
}
