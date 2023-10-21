using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Verse;

namespace Universum.Game {
    public class MainLoop : GameComponent {
        public static MainLoop instance;

        private TickManager _tickManager;
        public int tick = 0;
        public TimeSpeed timeSpeed = TimeSpeed.Paused;

        private Camera _camera;
        public Vector3 cameraPosition;

        private RimWorld.Planet.WorldCameraDriver _cameraDriver;
        public Vector3 currentSphereFocusPoint;

        private List<World.CelestialObject> _celestialObjects = new List<World.CelestialObject>();
        private readonly Dictionary<string, int> _objectGenerationSpawnTick = new Dictionary<string, int>();
        private int _spawnTickMin = 0;

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
        private List<int?> _exposeCelestialObjectDeathTicks = new List<int?>();

        public int seed = Rand.Int;

        private readonly Queue<World.CelestialObject> _visualGenerationQueue = new Queue<World.CelestialObject>();
        private Thread _visualGenerationWorker;

        public MainLoop(Verse.Game game) : base() {
            if (instance != null) {
                instance = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            instance = this;
        }

        ~MainLoop() => Destroy();

        public void Destroy() {
            for (int i = 0; i < _celestialObjects.Count; i++) _celestialObjects[i].Destroy();
        }

        public override void GameComponentTick() {
            if (_tickManager == null || _tickManager.TicksGame % 10 != 0) return;

            for (int i = 0; i < _totalCelestialObjectsCached; i++) _celestialObjectsCache[i].Tick();

            if (_tickManager.TicksGame < _spawnTickMin) return;

            foreach (Defs.ObjectGeneration objectGenerationStep in Defs.Loader.celestialObjectGenerationRandomSteps.Values) {
                if (!_objectGenerationSpawnTick.ContainsKey(objectGenerationStep.defName)) continue;
                int spawnTick = _objectGenerationSpawnTick[objectGenerationStep.defName];
                if (_tickManager.TicksGame > spawnTick) {
                    World.Generator.Generate(
                        objectGenerationStep,
                        despawnBetweenDays: objectGenerationStep.despawnBetweenDays,
                        amount: Rand.Range((int) objectGenerationStep.spawnAmountBetween[0], (int) objectGenerationStep.spawnAmountBetween[1])
                    );

                    int newSpawnTick = _GetSpawnTick(objectGenerationStep.spawnBetweenDays[0], objectGenerationStep.spawnBetweenDays[1]);
                    _spawnTickMin = newSpawnTick;

                    _objectGenerationSpawnTick[objectGenerationStep.defName] = newSpawnTick;
                }
            }

            foreach (var spawnTick in _objectGenerationSpawnTick.Values) if (spawnTick < _spawnTickMin) _spawnTickMin = spawnTick;
        }

        public override void GameComponentUpdate() {
            _GetFrameData();
            if (dirtyCache) _Recache();
            if (_wait && !forceUpdate) return;
            if (_frameChanged || forceUpdate) {
                _Update();
                _Render();
            }
            forceUpdate = false;
        }

        public void AddObject(List<World.CelestialObject> celestialObjects) {
            _celestialObjects.AddRange(celestialObjects);
            foreach (var celestialObject in celestialObjects) _visualGenerationQueue.Enqueue(celestialObject);
            dirtyCache = true;
        }

        public void AddObject(World.CelestialObject celestialObject) {
            _celestialObjects.Add(celestialObject);
            _visualGenerationQueue.Enqueue(celestialObject);
            dirtyCache = true;
        }

        private static void _ProcessVisualGenerationQueue(Queue<World.CelestialObject> queue) {
            int currentSeed = instance.seed;

            while (queue.Count > 0) {
                if (instance == null || currentSeed != instance.seed) return;
                World.CelestialObject celestialObject = queue.Dequeue();
                if (celestialObject == null) continue;
                celestialObject.GenerateVisuals();
            }
        }

        private void _GetFrameData() {
            if (_tickManager != null) {
                tick = _tickManager.TicksGame;
                timeSpeed = _tickManager.curTimeSpeed;
            }

            if (_camera != null) cameraPosition = _camera.transform.position;

            if (_cameraDriver != null) currentSphereFocusPoint = _cameraDriver.CurrentlyLookingAtPointOnSphere;

            if (_celestialObjects.NullOrEmpty() || _tickManager == null) {
                _wait = true;
                return;
            }
            _wait = !RimWorld.Planet.WorldRendererUtility.WorldRenderedNow && !forceUpdate;

            bool sceneIsWorld = RimWorld.Planet.WorldRendererUtility.WorldRenderedNow;
            bool sceneSwitched = _prevWorldSceneRendered != sceneIsWorld;
            _prevWorldSceneRendered = RimWorld.Planet.WorldRendererUtility.WorldRenderedNow;

            if (sceneSwitched) forceUpdate = true;

            worldSceneActivated = sceneSwitched && sceneIsWorld;
            worldSceneDeactivated = sceneSwitched && !sceneIsWorld;

            unpaused = _tickManager.TicksGame != _prevTick;
            _prevTick = _tickManager.TicksGame;

            cameraMoved = _camera.transform.position != _prevCameraPosition;
            _prevCameraPosition = _camera.transform.position;

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

            _tickManager = Find.TickManager;
            _camera = Find.WorldCamera.GetComponent<Camera>();
            _cameraDriver = Find.WorldCameraDriver;

            for (int i = 0; i < _celestialObjects.Count; i++) {
                if (_celestialObjects[i].ShouldDespawn()) {
                    _celestialObjects[i].Destroy();
                    _celestialObjects[i] = null;
                }
            }
            _celestialObjects = _celestialObjects.Where(item => item != null).ToList();

            _totalCelestialObjectsCached = _celestialObjects.Count;
            _celestialObjectsCache = new World.CelestialObject[_totalCelestialObjectsCached];

            for (int i = 0; i < _totalCelestialObjectsCached; i++) _celestialObjectsCache[i] = _celestialObjects[i];

            if (_tickManager != null || _camera != null || _cameraDriver != null) _Update();

            if (_objectGenerationSpawnTick.Count == 0) {
                foreach (var step in Defs.Loader.celestialObjectGenerationRandomSteps.Values) {
                    int spawnTick = _GetSpawnTick(step.spawnBetweenDays[0], step.spawnBetweenDays[1]);
                    if (spawnTick < _spawnTickMin) _spawnTickMin = spawnTick;

                    _objectGenerationSpawnTick.Add(
                        step.defName,
                        spawnTick
                    );
                }
            }

            if (_visualGenerationQueue.Count > 0 && (_visualGenerationWorker == null || !_visualGenerationWorker.IsAlive)) {
                Queue<World.CelestialObject> copiedQueue = new Queue<World.CelestialObject>(_visualGenerationQueue);
                _visualGenerationQueue.Clear();

                _visualGenerationWorker = new Thread(() => _ProcessVisualGenerationQueue(copiedQueue));
                _visualGenerationWorker.Start();
            }
        }

        private int _GetSpawnTick(float betweenDaysMin, float betweenDaysMax) => (int) Rand.Range(betweenDaysMin * 60000, betweenDaysMax * 60000) + _tickManager.TicksGame;

        public override void ExposeData() {
            if (Scribe.mode == LoadSaveMode.Saving) _SaveData();
            if (Scribe.mode == LoadSaveMode.LoadingVars) _LoadData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit) _PostLoadData();
        }

        private void _SaveData() {
            _exposeCelestialObjectDefNames.Clear();
            _exposeCelestialObjectSeeds.Clear();
            _exposeCelestialObjectPositions.Clear();
            _exposeCelestialObjectDeathTicks.Clear();

            for (int i = 0; i < _totalCelestialObjectsCached; i++) {
                _celestialObjectsCache[i].GetExposeData(
                    _exposeCelestialObjectDefNames,
                    _exposeCelestialObjectSeeds,
                    _exposeCelestialObjectPositions,
                    _exposeCelestialObjectDeathTicks
                );
            }

            Scribe_Collections.Look(ref _exposeCelestialObjectDefNames, "_exposeCelestialObjectDefNames", LookMode.Value);
            Scribe_Collections.Look(ref _exposeCelestialObjectSeeds, "_exposeCelestialObjectSeeds", LookMode.Value);
            Scribe_Collections.Look(ref _exposeCelestialObjectPositions, "_exposeCelestialObjectPositions", LookMode.Value);
            Scribe_Collections.Look(ref _exposeCelestialObjectDeathTicks, "_exposeCelestialObjectDeathTicks", LookMode.Value);

            _exposeCelestialObjectDefNames.Clear();
            _exposeCelestialObjectSeeds.Clear();
            _exposeCelestialObjectPositions.Clear();
            _exposeCelestialObjectDeathTicks.Clear();
        }

        private void _LoadData() {
            Scribe_Collections.Look(ref _exposeCelestialObjectDefNames, "_exposeCelestialObjectDefNames", LookMode.Value);
            Scribe_Collections.Look(ref _exposeCelestialObjectSeeds, "_exposeCelestialObjectSeeds", LookMode.Value);
            Scribe_Collections.Look(ref _exposeCelestialObjectPositions, "_exposeCelestialObjectPositions", LookMode.Value);
            Scribe_Collections.Look(ref _exposeCelestialObjectDeathTicks, "_exposeCelestialObjectDeathTicks", LookMode.Value);
        }

        private void _PostLoadData() {
            World.Generator.Create(
                _exposeCelestialObjectDefNames,
                _exposeCelestialObjectSeeds,
                _exposeCelestialObjectPositions,
                _exposeCelestialObjectDeathTicks
            );

            _exposeCelestialObjectDefNames.Clear();
            _exposeCelestialObjectSeeds.Clear();
            _exposeCelestialObjectPositions.Clear();
            _exposeCelestialObjectDeathTicks.Clear();
        }
    }

    [HarmonyPatch(typeof(Verse.Profile.MemoryUtility), "ClearAllMapsAndWorld")]
    public class MemoryUtility_ClearAllMapsAndWorld {
        public static void Prefix() {
            MainLoop.instance = null;
        }
    }
}
