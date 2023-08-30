using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace Universum.World {
    public abstract class CelestialObject {
        public Defs.CelestialObject def;

        public string name;

        private int _totalMeshes = 0;
        private Mesh[] _meshes = new Mesh[0];
        private Material[] _materials = new Material[0];
        private bool _generatingShape = false;

        public int seed;
        private Functionality.Random _rand;
        private Shape _shape;
        private Matrix4x4 _transformationMatrix = Matrix4x4.identity;
        private Quaternion _rotation = Quaternion.identity;
        public Quaternion billboardRotation = Quaternion.identity;
        private Quaternion _axialRotation = Quaternion.identity;
        private CelestialObject _orbitTarget;

        public Vector3 realPosition;
        public Vector3 currentPosition;
        public Vector3 size;
        public float extraSize;
        private float _orbitSpeed;
        private int _period;
        private int _timeOffset;
        private Vector3 _orbitPath;
        private Vector3 _orbitSpread;
        private int _orbitDirection;
        private float _axialRotationSpeed;
        private Vector3 _orbitPosition;

        private readonly List<ObjectComponent> _components = new List<ObjectComponent>();

        public CelestialObject(string celestialObjectDefName) {
            def = Defs.Loader.celestialObjects[celestialObjectDefName];
        }

        public virtual void Randomize() {
            Init();
        }

        public virtual void Init(int? seed = null, Vector3? currentPosition = null) {
            this.seed = seed ?? Rand.Int;
            _rand = new Functionality.Random(this.seed);

            Defs.NamePack namePack = Defs.Loader.namePacks[def.namePackDefName];
            name = $"{_rand.GetElement(namePack.prefix)}{_rand.GetElement(namePack.postfix)}";

            float size = _rand.GetValueBetween(def.sizeBetween);
            this.size = new Vector3(size, size, size);
            _orbitSpeed = _rand.GetValueBetween(def.orbitSpeedBetween);
            _period = (int) (36000.0f + (6000.0f * (_rand.GetFloat() - 0.5f)));
            _timeOffset = _rand.GetValueBetween(new Vector2Int(0, _period));
            _orbitPath = def.orbitPath;
            _orbitSpread = def.orbitSpread;
            _orbitPosition = new Vector3 {
                x = _orbitPath.x + (float) ((Rand.Value - 0.5f) * (_orbitPath.x * _orbitSpread.x)),
                y = _rand.GetValueBetween(new Vector2(Math.Abs(_orbitPath.y) * -1, Math.Abs(_orbitPath.y))),
                z = _orbitPath.z + (float) ((Rand.Value - 0.5f) * (_orbitPath.z * _orbitSpread.z))
            };
            switch (def.orbitDirection) {
                case Defs.OrbitDirection.LEFT:
                    _orbitDirection = -1;
                    break;
                case Defs.OrbitDirection.RIGHT:
                    _orbitDirection = 1;
                    break;
                case Defs.OrbitDirection.RANDOM:
                    _orbitDirection = _rand.GetBool() ? 1 : -1;
                    break;
                default:
                    _orbitDirection = 1;
                    break;
            }
            _axialRotationSpeed = _rand.GetValueBetween(def.axialRotationSpeedBetween);

            this.currentPosition = currentPosition ?? _orbitPath;
        }

        public virtual void OnWorldSceneActivated() {
            foreach (var component in _components) component.OnWorldSceneActivated();
        }

        public virtual void OnWorldSceneDeactivated() {
            foreach (var component in _components) component.OnWorldSceneDeactivated();
        }

        public virtual void Update() {
            UpdateRotation(Game.MainLoop.tickManager.TicksGame, Game.MainLoop.cameraDriver.CurrentlyLookingAtPointOnSphere);
            UpdateTransformationMatrix();
            foreach(var component in _components) component.Update();
        }

        public virtual void OnUnpaused() {
            UpdatePosition(Game.MainLoop.tickManager.TicksGame);
        }

        public virtual void OnCameraMoved() { }

        public virtual void UpdatePosition(int tick) {
            float time = _orbitSpeed * _orbitDirection * tick + _timeOffset;
            float angularFrequencyTime = 6.28f / _period * time;
            float yOffset = _orbitPosition.y / 2;
            currentPosition.x = (_orbitPosition.x - yOffset) * (float) Math.Cos(angularFrequencyTime);
            currentPosition.z = (_orbitPosition.z - yOffset) * (float) Math.Sin(angularFrequencyTime);
        }

        public virtual void UpdateRotation(int tick, Vector3 center) {
            Vector3 towards_camera = Vector3.Cross(center, Vector3.up);
            billboardRotation = Quaternion.LookRotation(towards_camera, center);

            _axialRotation = Quaternion.Euler(0.5f * _axialRotationSpeed * tick * _orbitDirection * -1, _axialRotationSpeed * tick * _orbitDirection * -1, 0);

            _rotation = _axialRotation;
        }

        public virtual void UpdateTransformationMatrix() {
            _transformationMatrix.SetTRS(currentPosition, _rotation, size);
            // update real position
            realPosition.x = _transformationMatrix.m03;
            realPosition.y = _transformationMatrix.m13;
            realPosition.z = _transformationMatrix.m23;
        }

        public virtual void Render() {
            for (int i = 0; i < _totalMeshes; i++) {
                Graphics.Internal_DrawMesh(
                    _meshes[i],
                    submeshIndex: 0,
                    _transformationMatrix,
                    _materials[i],
                    RimWorld.Planet.WorldCameraManager.WorldLayer,
                    camera: null,
                    properties: null,
                    ShadowCastingMode.On,
                    receiveShadows: true,
                    probeAnchor: null,
                    lightProbeUsage: LightProbeUsage.BlendProbes,
                    lightProbeProxyVolume: null
                );
            }
            foreach (var component in _components) component.Render();
        }

        public virtual void Recache() {
            if (_generatingShape || _shape == null) return;
            _meshes = (Mesh[]) _shape.GetMeshes().Clone();
            _materials = (Material[]) _shape.GetMaterials().Clone();
            _totalMeshes = _meshes.Length;
            extraSize = _shape.highestElevation;
            _shape = null;

            foreach (var componentDef in def.components) {
                ObjectComponent component = (ObjectComponent) Activator.CreateInstance(
                    componentDef.componentClass,
                    new object[] { this, componentDef }
                );
                _components.Add(component);
            }
        }

        public virtual Vector3 GetOrbitAroundPosition() {
            return _orbitTarget?.realPosition ?? Vector3.zero;
        }

        public virtual void SetOrbitTarget(CelestialObject target) {
            _orbitTarget = target;
        }

        public virtual void GenerateVisuals() {
            if (_totalMeshes != 0) return;
            if (def.shape != null) {
                _GenerateShape();
            } else if (def.icon != null) {

            } else {
                Logger.print(
                    Logger.Importance.Error,
                    text: $"Celestial object def '{def.defName}' doesn't contain either a Icon or Shape specification",
                    prefix: Style.name_prefix
                );
                return;
            }
        }

        private void _GenerateShape() {
            _generatingShape = true;

            _shape = new Shape(seed);

            foreach (Defs.Mesh mesh in def.shape.meshes) {
                List<bool> isMask = new List<bool>();
                List<bool> useMask = new List<bool>();
                List<float> noiseStrength = new List<float>();
                List<float> noiseRoughness = new List<float>();
                List<int> noiseIterations = new List<int>();
                List<float> noisePersistence = new List<float>();
                List<float> noiseBaseRoughness = new List<float>();
                List<float> noiseMinValue = new List<float>();

                foreach (Defs.Noise noiseLayer in mesh.noiseLayers) {
                    isMask.Add(noiseLayer.isMask);
                    useMask.Add(noiseLayer.useMask);
                    noiseStrength.Add(_rand.GetValueBetween(noiseLayer.strenghtBetween));
                    noiseRoughness.Add(_rand.GetValueBetween(noiseLayer.roughnessBetween));
                    noiseIterations.Add(_rand.GetValueBetween(new Vector2Int((int) Math.Abs(noiseLayer.iterationsBetween.x), (int) Math.Abs(noiseLayer.iterationsBetween.y))));
                    noisePersistence.Add(_rand.GetValueBetween(noiseLayer.persistenceBetween));
                    noiseBaseRoughness.Add(_rand.GetValueBetween(noiseLayer.baseRoughnessBetween));
                    noiseMinValue.Add(_rand.GetValueBetween(noiseLayer.minNoiseValueBetween));
                }

                _shape.Add(
                    Assets.materials[mesh.materialDefName],
                    mesh.type,
                    mesh.subdivisionIterations,
                    mesh.detail,
                    mesh.radius,
                    mesh.dimensions,
                    mesh.minElevationColor,
                    mesh.maxElevationColor,
                    isMask,
                    useMask,
                    noiseStrength,
                    noiseRoughness,
                    noiseIterations,
                    noisePersistence,
                    noiseBaseRoughness,
                    noiseMinValue
                );
            }

            _generatingShape = false;
        }
    }
}
