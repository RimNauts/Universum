using System.Collections.Generic;
using Verse;

namespace Universum.Utilities.Biome {
    public class Properties : DefModExtension {
        public List<string> allowed_utilities = new List<string>();
        public float temperature = 0.0f;
        private static readonly Dictionary<string, Properties> DefaultsByPackageId = new Dictionary<string, Properties>();
        private static readonly Dictionary<string, Properties> DefaultsByDefName = new Dictionary<string, Properties>();

        public static Properties[] GetAll() {
            Properties[] biomePropertiesArray = new Properties[DefDatabase<RimWorld.BiomeDef>.DefCount];
            foreach (RimWorld.BiomeDef biome in DefDatabase<RimWorld.BiomeDef>.AllDefs) {
                Properties biomeProperties = new Properties();
                try {
                    biomeProperties = ((biome.GetModExtension<Properties>() ?? DefaultsByDefName.TryGetValue(biome.defName)) ?? DefaultsByPackageId.TryGetValue(biome.modContentPack.ModMetaData.PackageIdNonUnique)) ?? new Properties();
                } catch { }
                biomePropertiesArray[biome.index] = biomeProperties;
            }
            return biomePropertiesArray;
        }
    }
}
