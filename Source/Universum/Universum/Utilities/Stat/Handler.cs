using System.Linq;

namespace Universum.Utilities.Stat {
    public static class Handler {
        private static int total_stats_found;
        private static int total_configurations_found;
        private static Properties[] stats;

        public static Properties get_properties(this RimWorld.StatDef stat_def) {
            try {
                return stats[stat_def.index];
            } catch {
                return new Properties();
            }
        }

        public static void init() {
            stats = Properties.GetAll();
            // set stats
            total_stats_found = stats.Count();
            total_configurations_found = 0;
            foreach (Properties stat in stats) {
                total_configurations_found += stat.allowed_utilities.Count();
            }
            // print stats
            Logger.print(
                Logger.Importance.Info,
                key: "Universum.Info.stat_handler_done",
                prefix: Style.tab,
                args: new Verse.NamedArgument[] { total_stats_found, total_configurations_found }
            );
        }
    }
}
