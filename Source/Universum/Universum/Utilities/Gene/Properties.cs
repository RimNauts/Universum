using System.Collections.Generic;
using Verse;

namespace Universum.Utilities.Gene {
    public class Properties : DefModExtension {
        public List<string> allowed_utilities = new List<string>();
        private static readonly Dictionary<string, Properties> DefaultsByPackageId = new Dictionary<string, Properties>();
        private static readonly Dictionary<string, Properties> DefaultsByDefName = new Dictionary<string, Properties>();

        public static Properties[] GetAll() {
            Properties[] genePropertiesArray = new Properties[DefDatabase<GeneDef>.DefCount];
            foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefs) {
                Properties statProperties = new Properties();
                try {
                    statProperties = ((gene.GetModExtension<Properties>() ?? DefaultsByDefName.TryGetValue(gene.defName)) ?? DefaultsByPackageId.TryGetValue(gene.modContentPack.ModMetaData.PackageIdNonUnique)) ?? new Properties();
                } catch { }
                genePropertiesArray[gene.index] = statProperties;
            }
            return genePropertiesArray;
        }
    }
}
