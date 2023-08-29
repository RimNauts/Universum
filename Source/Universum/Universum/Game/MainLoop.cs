using UnityEngine;
using Verse;

namespace Universum.Game {
    [StaticConstructorOnStartup]
    public class MainLoop : GameComponent {
        public static Verse.Game state;
        public static TickManager tickManager;
        public static Camera camera;
        public static RimWorld.Planet.WorldCameraDriver cameraDriver;

        public MainLoop(Verse.Game game) : base() {
            state = game;
        }

        public override void FinalizeInit() {
            tickManager = Find.TickManager;
            camera = Find.WorldCamera.GetComponent<Camera>();
            cameraDriver = Find.WorldCameraDriver;
        }
    }
}
