using System.Linq;
using Verse;

namespace Universum.Utilities.Gene {
    public static class Handler {
        private static int total_genes_found;
        private static int total_configurations_found;
        private static Properties[] genes;

        public static Properties get_properties(this GeneDef gene_def) {
            try {
                return genes[gene_def.index];
            } catch {
                return new Properties();
            }
        }

        public static void init() {
            genes = Properties.GetAll();
            // set stats
            total_genes_found = genes.Count();
            total_configurations_found = 0;
            foreach (Properties gene in genes) {
                total_configurations_found += gene.allowed_utilities.Count();
            }
            // print stats
            Logger.print(
                Logger.Importance.Info,
                key: "Universum.Info.gene_handler_done",
                prefix: Style.tab,
                args: new NamedArgument[] { total_genes_found, total_configurations_found }
            );
        }
    }
}
