using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Universum.Game {
    [StaticConstructorOnStartup]
    public class MainLoop : GameComponent {
        public static MainLoop instance;

        public static Verse.Game state;
        public static TickManager tickManager;
        public static Camera camera;
        public static RimWorld.Planet.WorldCameraDriver cameraDriver;

        private readonly List<World.CelestialObject> _celestialObjects = new List<World.CelestialObject>();

        private int _totalCelestialObjectsCached = 0;
        private World.CelestialObject[] _celestialObjectsCache = new World.CelestialObject[0];
        public bool dirtyCache = false;

        private bool _wait = false;
        public bool forceUpdate = true;
        private bool _prevWorldSceneRendered = false;
        public bool worldSceneActivated = false;
        public bool worldSceneDeactivated = false;
        public bool unpaused = false;
        private int _prevTick;
        public bool cameraMoved = false;
        private Vector3 _prevCameraPosition;
        private bool _frameChanged = false;

        private List<string> _exposeCelestialObjectDefNames = new List<string>();
        private List<int?> _exposeCelestialObjectSeeds = new List<int?>();
        private List<Vector3?> _exposeCelestialObjectPositions = new List<Vector3?>();

        public MainLoop(Verse.Game game) : base() {
            state = game;
            instance = this;
        }

        public override void ExposeData() {
            if (Scribe.mode == LoadSaveMode.Saving) {
                _exposeCelestialObjectDefNames.Clear();
                _exposeCelestialObjectSeeds.Clear();
                _exposeCelestialObjectPositions.Clear();

                for (int i = 0; i < _totalCelestialObjectsCached; i++) {
                    _exposeCelestialObjectDefNames.Add(_celestialObjectsCache[i].def.defName);
                    _exposeCelestialObjectSeeds.Add(_celestialObjectsCache[i].seed);
                    _exposeCelestialObjectPositions.Add(_celestialObjectsCache[i].currentPosition);
                }
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.Saving) {
                Scribe_Collections.Look(ref _exposeCelestialObjectDefNames, "_exposeCelestialObjectDefNames", LookMode.Value);
                Scribe_Collections.Look(ref _exposeCelestialObjectSeeds, "_exposeCelestialObjectSeeds", LookMode.Value);
                Scribe_Collections.Look(ref _exposeCelestialObjectPositions, "_exposeCelestialObjectPositions", LookMode.Value);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                World.Generator.Create(_exposeCelestialObjectDefNames, _exposeCelestialObjectSeeds, _exposeCelestialObjectPositions);

                _exposeCelestialObjectDefNames.Clear();
                _exposeCelestialObjectSeeds.Clear();
                _exposeCelestialObjectPositions.Clear();
            }
        }

        public override void GameComponentUpdate() {
            _GetFrameData();
            if (dirtyCache) _Recache();
            if (_wait && !forceUpdate) return;
            if (_frameChanged || forceUpdate) _Update();
            _Render();
        }

        public void AddObject(List<World.CelestialObject> celestialObjects) {
            _celestialObjects.AddRange(celestialObjects);
            dirtyCache = true;
        }

        public void AddObject(World.CelestialObject celestialObject) {
            _celestialObjects.Add(celestialObject);
            dirtyCache = true;
        }

        private void _GetFrameData() {
            if (_celestialObjects.NullOrEmpty() || tickManager == null) {
                _wait = true;
                return;
            }
            _wait = !RimWorld.Planet.WorldRendererUtility.WorldRenderedNow && !forceUpdate;
            if (!_wait) forceUpdate = false;

            bool sceneIsWorld = RimWorld.Planet.WorldRendererUtility.WorldRenderedNow;
            bool sceneSwitched = _prevWorldSceneRendered != sceneIsWorld;
            _prevWorldSceneRendered = RimWorld.Planet.WorldRendererUtility.WorldRenderedNow;

            if (sceneSwitched) forceUpdate = true;

            worldSceneActivated = sceneSwitched && sceneIsWorld;
            worldSceneDeactivated = sceneSwitched && !sceneIsWorld;

            unpaused = tickManager.TicksGame != _prevTick;
            _prevTick = tickManager.TicksGame;

            cameraMoved = camera.transform.position != _prevCameraPosition;
            _prevCameraPosition = camera.transform.position;

            _frameChanged = unpaused || cameraMoved;
        }

        private void _Update() {
            for (int i = 0; i < _totalCelestialObjectsCached; i++) _celestialObjectsCache[i].Update();

            //Parallel.For(0, _totalCelestialObjectsCached, new ParallelOptions { MaxDegreeOfParallelism = 4 }, i => { _celestialObjectsCache[i].Update(); });
        }

        private void _Render() {
            for (int i = 0; i < _totalCelestialObjectsCached; i++) _celestialObjectsCache[i].Render();
        }

        private void _Recache() {
            dirtyCache = false;
            forceUpdate = true;

            tickManager = Find.TickManager;
            camera = Find.WorldCamera.GetComponent<Camera>();
            cameraDriver = Find.WorldCameraDriver;

            _totalCelestialObjectsCached = _celestialObjects.Count;
            _celestialObjectsCache = new World.CelestialObject[_totalCelestialObjectsCached];

            for (int i = 0; i < _totalCelestialObjectsCached; i++) {
                _celestialObjectsCache[i] = _celestialObjects[i];
                _celestialObjectsCache[i].Recache();
            }

            if (tickManager != null || camera != null || cameraDriver != null) _Update();
        }
    }
}
