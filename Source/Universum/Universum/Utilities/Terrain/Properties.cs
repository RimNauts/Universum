using System.Collections.Generic;
using Verse;

namespace Universum.Utilities.Terrain {
    public class Properties : Verse.DefModExtension {
        public List<string> allowed_utilities = new List<string>();
        private static readonly Dictionary<string, Properties> DefaultsByPackageId = new Dictionary<string, Properties>();
        private static readonly Dictionary<string, Properties> DefaultsByDefName = new Dictionary<string, Properties>();

        public static Properties[] GetAll() {
            Properties[] terrainPropertiesArray = new Properties[Verse.DefDatabase<Verse.TerrainDef>.DefCount];
            foreach (Verse.TerrainDef terrain in Verse.DefDatabase<Verse.TerrainDef>.AllDefs) {
                Properties terrainProperties = new Properties();
                try {
                    terrainProperties = ((terrain.GetModExtension<Properties>() ?? DefaultsByDefName.TryGetValue(terrain.defName)) ?? DefaultsByPackageId.TryGetValue(terrain.modContentPack.ModMetaData.PackageIdNonUnique)) ?? new Properties();
                } catch { /* can't find mod options for carpets and stone terrian as they are auto-generated */ }
                terrainPropertiesArray[terrain.index] = terrainProperties; 
            }
            return terrainPropertiesArray;
        }
    }
}
