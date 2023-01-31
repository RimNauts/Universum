using Verse;

namespace Universum {
    public static class Logger {
        public enum Importance {
            Info = 0,
            Warning = 1,
            Error = 2,
        }

        public static void print(
            Importance importance,
            string key,
            string prefix = null,
            NamedArgument[] args = null
        ) {
            string message;
            if (args == null) {
                message = TranslatorFormattedStringExtensions.Translate(key);
            } else {
                message = TranslatorFormattedStringExtensions.Translate(key, args);
            }
            print(importance, text: message, prefix);
        }

        public static void print(Importance importance, string text, string prefix = null) {
            if (prefix != null) text = prefix + text;
            switch (importance) {
                case Importance.Info:
                    Log.Message(text);
                    return;
                case Importance.Warning:
                    Log.Warning(text);
                    return;
                case Importance.Error:
                    Log.Error(text);
                    return;
            }
        }
    }
}
