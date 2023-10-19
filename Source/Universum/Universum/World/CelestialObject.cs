using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Universum.World {
    public abstract class CelestialObject {
        public int seed;
        public string name;
        public Defs.CelestialObject def;

        protected Functionality.Random _rand;

        protected bool _generatingShape = false;
        protected bool _dirty = false;
        protected bool _positionChanged = true;
        protected bool _rotationChanged = true;
        protected bool _scaleChanged = true;

        public int? deathTick = null;

        protected Shape _shape;
        protected Matrix4x4 _transformationMatrix = Matrix4x4.identity;
        protected Quaternion _rotation = Quaternion.identity;
        public Quaternion billboardRotation = Quaternion.identity;
        protected Quaternion _axialRotation = Quaternion.identity;
        protected CelestialObject _target;

        protected Transform[] _transforms = new Transform[0];
        protected ObjectComponent[] _components = new ObjectComponent[0];

        public Vector3 transformedPosition;
        public Vector3 position;
        public Vector3 scale;
        protected float _scalePercentage;
        public float extraScale;
        public float speed;
        protected float _speedPercentage;
        protected int _period;
        protected int _timeOffset;
        protected float _orbitPathOffsetPercentage;
        protected float _orbitEccentricity;
        protected float _orbitSpread;
        protected int _orbitDirection;
        protected float _axialRotationSpeed;
        protected float _orbitRadius;
        protected float _yOffset;

        public CelestialObject(string celestialObjectDefName) {
            def = Defs.Loader.celestialObjects[celestialObjectDefName];
        }

        ~CelestialObject() => Destroy();

        public virtual void Destroy() {
            for (int i = 0; i < _components.Length; i++) _components[i].Destroy();
            for (int i = 0; i < _transforms.Length; i++) UnityEngine.Object.Destroy(_transforms[i].gameObject);
        }

        public virtual void GetExposeData(List<string> defNames, List<int?> seeds, List<Vector3?> positions, List<int?> deathTicks) {
            defNames.Add(def.defName);
            seeds.Add(seed);
            positions.Add(position);
            deathTicks.Add(deathTick);
        }

        public virtual void Randomize() {
            Init(deathTick: deathTick);
        }

        public virtual void Init(int? seed = null, Vector3? position = null, int? deathTick = null) {
            this.seed = seed ?? Rand.Int;
            _rand = new Functionality.Random(this.seed);

            this.deathTick = deathTick;

            Defs.NamePack namePack = Defs.Loader.namePacks[def.namePackDefName];
            name = $"{_rand.GetElement(namePack.prefix)}-{_rand.GetElement(namePack.postfix)}";

            _scalePercentage = _rand.GetValueBetween(def.scalePercentageBetween);
            UpdateScale();
            _speedPercentage = _rand.GetValueBetween(def.speedPercentageBetween);
            UpdateSpeed();
            _period = (int) (36000.0f + (6000.0f * (_rand.GetFloat() - 0.5f)));
            _timeOffset = _rand.GetValueBetween(new Vector2Int(0, _period));
            _orbitPathOffsetPercentage = def.orbitPathOffsetPercentage;
            _orbitEccentricity = _rand.GetValueBetween(def.orbitEccentricityBetween);
            _orbitSpread = _rand.GetValueBetween(def.orbitSpreadBetween);
            UpdateOrbitRadius();
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

            if (position != null) {
                this.position = (Vector3) position;
            } else {
                UpdatePosition(tick: 0);
                this.position.y = _rand.GetValueBetween(def.yOffsetBetween);
            }
        }

        public virtual void Tick() {
            if (ShouldDespawn()) Game.MainLoop.instance.dirtyCache = true;
        }

        public virtual void Update() {
            if (Game.MainLoop.instance.unpaused || Game.MainLoop.instance.forceUpdate) UpdatePosition(Game.MainLoop.tickManager.TicksGame);
            UpdateRotation(Game.MainLoop.tickManager.TicksGame, Game.MainLoop.cameraDriver.CurrentlyLookingAtPointOnSphere);
            UpdateTransformationMatrix();

            for (int i = 0; i < _components.Length; i++) {
                _components[i].Update();
                if (Game.MainLoop.instance.worldSceneActivated) _components[i].OnWorldSceneActivated();
                if (Game.MainLoop.instance.worldSceneDeactivated) _components[i].OnWorldSceneDeactivated();
            }
        }

        public virtual void UpdatePosition(int tick) {
            _positionChanged = true;

            float time = speed * tick + _timeOffset;
            float angularFrequencyTime = 6.28f / _period * time;

            position.x = _orbitDirection * _orbitRadius * (float) Math.Cos(angularFrequencyTime);
            position.z = _orbitDirection * _orbitRadius * Mathf.Sqrt(1 - _orbitEccentricity * _orbitEccentricity) * (float) Math.Sin(angularFrequencyTime);
        }

        public virtual void UpdateRotation(int tick, Vector3 center) {
            _rotationChanged = true;

            Vector3 towards_camera = Vector3.Cross(center, Vector3.up);
            billboardRotation = Quaternion.LookRotation(towards_camera, center);

            _axialRotation = Quaternion.Euler(0.5f * _axialRotationSpeed * tick * _orbitDirection * -1, _axialRotationSpeed * tick * _orbitDirection * -1, 0);

            _rotation = _axialRotation;
        }

        public virtual void UpdateTransformationMatrix() {
            _transformationMatrix.SetTRS(position + GetTargetPosition(), _rotation, scale);
            // update real position
            transformedPosition.x = _transformationMatrix.m03;
            transformedPosition.y = _transformationMatrix.m13;
            transformedPosition.z = _transformationMatrix.m23;
        }

        public virtual void Render() {
            if (_dirty) _Recache();

            foreach (ObjectComponent component in _components) component.Render();

            for (int i = 0; i < _transforms.Length; i++) {
                if (_positionChanged || Game.MainLoop.instance.forceUpdate) _transforms[i].localPosition = position + GetTargetPosition();
                if (_rotationChanged || Game.MainLoop.instance.forceUpdate) _transforms[i].localRotation = _rotation;
                if (_scaleChanged || Game.MainLoop.instance.forceUpdate) _transforms[i].localScale = scale;
            }

            _positionChanged = false;
            _rotationChanged = false;
            _scaleChanged = false;
        }

        protected virtual void _Recache() {
            _dirty = false;
            if (_generatingShape || _shape == null) return;

            Mesh[] meshes = (Mesh[]) _shape.GetMeshes().Clone();
            Material[] materials = (Material[]) _shape.GetMaterials().Clone();
            extraScale = _shape.highestElevation;
            _shape = null;

            _transforms = new Transform[meshes.Length];
            for (int i = 0; i < meshes.Length; i++) {
                GameObject newGameObject = new GameObject {
                    layer = RimWorld.Planet.WorldCameraManager.WorldLayer
                };

                MeshFilter meshFilter = newGameObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();

                meshFilter.mesh = meshes[i];
                meshRenderer.material = materials[i];

                _transforms[i] = newGameObject.transform;
            }

            List<ObjectComponent> objectComponents = new List<ObjectComponent>();
            foreach (var componentDef in def.components) {
                ObjectComponent component = (ObjectComponent) Activator.CreateInstance(
                    componentDef.componentClass,
                    new object[] { this, componentDef }
                );
                objectComponents.Add(component);
            }

            _components = objectComponents.ToArray();

            Game.MainLoop.instance.forceUpdate = true;
        }

        public virtual void SetTarget(CelestialObject target) {
            _target = target;

            UpdateScale();
            UpdateOrbitRadius();
            UpdateSpeed();
        }

        public virtual void UpdateOrbitRadius() {
            Vector3 scaledOrbitOffset = GetTargetScale() * _orbitPathOffsetPercentage;
            _orbitRadius = scaledOrbitOffset.x + (float) ((_rand.GetFloat() - 0.5f) * (scaledOrbitOffset.x * _orbitSpread));
        }

        public virtual void UpdateScale() {
            _scaleChanged = true;

            Vector3 orbitAroundScale = GetTargetScale();
            scale.x = orbitAroundScale.x * _scalePercentage;
            scale.y = orbitAroundScale.y * _scalePercentage;
            scale.z = orbitAroundScale.z * _scalePercentage;
        }

        public virtual void UpdateSpeed() {
            speed = GetTargetSpeed() * _speedPercentage;
        }

        public virtual Vector3 GetTargetPosition() {
            return _target?.transformedPosition ?? Vector3.zero;
        }

        public virtual float GetTargetSpeed() {
            return _target?.speed ?? 0.8f;
        }

        public virtual Vector3 GetTargetScale() {
            return _target?.scale ?? new Vector3(100.0f, 100.0f, 100.0f);
        }

        public virtual bool ShouldDespawn() => deathTick != null && Game.MainLoop.tickManager.TicksGame > deathTick;

        public virtual void GenerateVisuals() {
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

        protected virtual void _GenerateShape() {
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

            _shape.CompressData();

            _generatingShape = false;
            _dirty = true;
        }
    }
}
