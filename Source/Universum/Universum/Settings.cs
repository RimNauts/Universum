using System.Collections.Generic;

namespace Universum {
    public class Settings : Verse.ModSettings {
        private static int total_configurations_found;
        public static Dictionary<string, ObjectsDef.Metadata> utilities = new Dictionary<string, ObjectsDef.Metadata>();
        public static Dictionary<string, bool> failed_attempts = new Dictionary<string, bool>();
        public static Dictionary<string, bool> saved_settings = new Dictionary<string, bool>();

        public static void init() {
            // check with defs to update list
            foreach (ObjectsDef.Metadata metadata in DefOf.Objects.Utilities) utilities.Add(metadata.id, metadata);
            foreach (KeyValuePair<string, bool> saved_utility in saved_settings) {
                if (utilities.ContainsKey(saved_utility.Key)) utilities[saved_utility.Key].toggle = saved_utility.Value;
            }
            total_configurations_found = utilities.Count;
            // print stats
            Logger.print(
                Logger.Importance.Info,
                key: "Universum.Info.settings_loader_done",
                prefix: Style.tab,
                args: new Verse.NamedArgument[] { total_configurations_found }
            );
        }

        public static bool utility_turned_on(string id) {
            if (utilities.TryGetValue(id, out ObjectsDef.Metadata utility)) {
                return utility.toggle;
            } else {
                if (failed_attempts.TryGetValue(id, out bool value)) return value;
                Logger.print(
                    Logger.Importance.Error,
                    key: "Universum.Error.failed_to_find_utility",
                    prefix: Style.name_prefix,
                    args: new Verse.NamedArgument[] { id }
                );
                failed_attempts.Add(id, false);
                return false;
            }
        }

        public override void ExposeData() {
            base.ExposeData();
            Verse.Scribe_Collections.Look(ref saved_settings, "saved_settings", Verse.LookMode.Value, Verse.LookMode.Value);
            if (saved_settings == null) saved_settings = new Dictionary<string, bool>();
        }
    }

    public class Settings_Page : Verse.Mod {
        public static Settings settings;
        private UnityEngine.Vector2 scrollpos = UnityEngine.Vector2.zero;

        public Settings_Page(Verse.ModContentPack content) : base(content) => settings = GetSettings<Settings>();

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect) {
            // default button
            UnityEngine.Rect buttons_rectangle = new UnityEngine.Rect(inRect.x, inRect.y + 24f, inRect.width, inRect.height - 24f);
            Verse.Listing_Standard buttons_view = new Verse.Listing_Standard();
            buttons_view.Begin(buttons_rectangle);
            if (buttons_view.ButtonText(Verse.TranslatorFormattedStringExtensions.Translate("Universum.default"))) {
                foreach (KeyValuePair<string, ObjectsDef.Metadata> utility in Settings.utilities) Settings.utilities[utility.Key].toggle = Settings.utilities[utility.Key].default_toggle;
            }
            buttons_view.End();
            // table header
            UnityEngine.Rect table_header_rectangle = new UnityEngine.Rect(buttons_rectangle.x, buttons_rectangle.y + 34f, buttons_rectangle.width, 30f);
            Verse.Listing_Standard table_header_view = new Verse.Listing_Standard();
            Verse.Widgets.DrawHighlight(table_header_rectangle);
            table_header_view.Begin(table_header_rectangle);
            table_header_view.Gap(5f);
            table_header_view.ColumnWidth = 460f;
            table_header_view.Label(Verse.TranslatorFormattedStringExtensions.Translate("Universum.utilities"));
            table_header_view.NewColumn();
            table_header_view.Gap(5f);
            table_header_view.ColumnWidth = 100f;
            table_header_view.Label(Verse.TranslatorFormattedStringExtensions.Translate("Universum.enabled"));
            table_header_view.End();
            // table content
            UnityEngine.Rect table_content_rectangle = new UnityEngine.Rect(buttons_rectangle.x, buttons_rectangle.y - 64f, buttons_rectangle.width, Settings.utilities.Count * 38f);
            UnityEngine.Rect viewRect = new UnityEngine.Rect(0.0f, 0.0f, 100f, Settings.utilities.Count * 30f);
            Verse.Widgets.BeginScrollView(new UnityEngine.Rect(buttons_rectangle.x, buttons_rectangle.y + 64f, buttons_rectangle.width, 484f), ref scrollpos, viewRect);
            Verse.Listing_Standard table_header_content = new Verse.Listing_Standard();
            table_header_content.Begin(table_content_rectangle);
            table_header_content.verticalSpacing = 8f;
            table_header_content.ColumnWidth = 500f;
            table_header_content.Gap(4f);
            foreach (KeyValuePair<string, ObjectsDef.Metadata> utility in Settings.utilities) {
                bool checkOn = utility.Value.toggle;
                string mod_name = "Unknown source";
                if (utility.Value.mod_name.Length > 0) mod_name = utility.Value.mod_name;
                string utility_name = utility.Key;
                if (utility.Value.label_key.Length > 0) {
                    try {
                        utility_name = Verse.TranslatorFormattedStringExtensions.Translate(utility.Value.label_key);
                    } catch { /* couldn't find the language key provided */ }
                }
                string label = "(" + mod_name + ") " + utility_name;
                string utility_description = null;
                if (utility.Value.description_key.Length > 0) {
                    try {
                        utility_description = Verse.TranslatorFormattedStringExtensions.Translate(utility.Value.description_key);
                    } catch { /* couldn't find the language key provided */ }
                }
                table_header_content.CheckboxLabeled(label, ref checkOn, tooltip: utility_description);
                Settings.utilities[utility.Key].toggle = checkOn;
            }
            table_header_content.End();
            Verse.Widgets.EndScrollView();
        }

        public override string SettingsCategory() => "Universum";

        public override void WriteSettings() {
            Settings.saved_settings = new Dictionary<string, bool>();
            foreach (KeyValuePair<string, ObjectsDef.Metadata> utility in Settings.utilities) {
                Settings.saved_settings.Add(utility.Value.id, utility.Value.toggle);
            }
            base.WriteSettings();
        }
    }
}
