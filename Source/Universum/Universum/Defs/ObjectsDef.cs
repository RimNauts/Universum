using System.Collections.Generic;
using Verse;

namespace Universum {
    public class ObjectsDef : Def {
        public List<Metadata> Utilities;
        public class Metadata {
            public string id;
            public string mod_name;
            public string label_key;
            public string description_key;
            public bool default_toggle = true;
            public bool toggle;
            public bool hide_in_settings = false;

            public Metadata() {
                toggle = default_toggle;
            }
        }
    }
}
