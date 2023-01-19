using System.Collections.Generic;
using Verse;

namespace Universum.Utilities.Biome {
    public class Properties : Verse.DefModExtension {
        public List<string> allowed_utilities = new List<string>();
        public float temperature = 0.0f;
        private static readonly Dictionary<string, Properties> DefaultsByPackageId = new Dictionary<string, Properties>();
        private static readonly Dictionary<string, Properties> DefaultsByDefName = new Dictionary<string, Properties>();

        public static Properties[] GetAll() {
            Properties[] biomePropertiesArray = new Properties[Verse.DefDatabase<RimWorld.BiomeDef>.DefCount];
            foreach (RimWorld.BiomeDef biome in Verse.DefDatabase<RimWorld.BiomeDef>.AllDefs) {
                Properties biomeProperties = ((biome.GetModExtension<Properties>() ?? DefaultsByDefName.TryGetValue(biome.defName)) ?? DefaultsByPackageId.TryGetValue(biome.modContentPack.ModMetaData.PackageIdNonUnique)) ?? new Properties();
                biomePropertiesArray[biome.index] = biomeProperties;
            }
            return biomePropertiesArray;
        }
    }
}
