namespace Universum {
    public static class CameraInfo {
        public static readonly float maxAltitude = 1600.0f;
        public static readonly float minAltitude = 80.0f;
        public static readonly float fieldOfView = 40.0f;
        public static readonly float zoomEnumMultiplier = 0.2f;
        public static readonly float zoomSensitivityMultiplier = 0.75f;
        public static readonly float dragSensitivityMultiplier = 0.5f;
        public static readonly float dragVelocityMultiplier = 0.50f;
    }

    public static class Info {
        public static readonly string name = "Universum";
        public static readonly string version = "2.3.2";
    }

    public static class Style {
        public static readonly string name_prefix = Info.name + ": ";
        public static readonly string tab = "        ";
    }
}
