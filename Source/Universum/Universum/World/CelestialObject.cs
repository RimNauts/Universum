using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Universum.World {
    public abstract class CelestialObject {
        public Defs.CelestialObject celestialObjectDef;

        public string name;

        private int _seed;
        private Functionality.Random _rand;
        private Shape _shape;
        private Matrix4x4 _transformationMatrix = Matrix4x4.identity;
        private Quaternion _rotation = Quaternion.identity;
        private Quaternion _billboardingRotation = Quaternion.identity;
        private Quaternion _axialRotation = Quaternion.identity;
        private CelestialObject _orbitTarget;

        public Vector3 realPosition;
        private Vector3 _currentPosition;
        private Vector3 _size;
        private float _extraSize;
        private float _orbitSpeed;
        private int _period;
        private int _timeOffset;
        private Vector3 _orbitPath;
        private Vector3 _orbitSpread;
        private int _orbitDirection;
        private float _axialRotationSpeed;
        private Vector3 _orbitPosition;

        private readonly TMPro.TextMeshPro _textComponent;
        private float _textComponentHideAtMinAltitude;
        private float _textComponentHideAtMaxAltitude;
        private bool _textComponentBlock;
        private bool _textComponentActive;

        private readonly TrailRenderer _trailComponent;
        private bool _trailComponentActive;
        private TimeSpeed _prevSpeed;
        private float _trailLength;
        private bool _firstRender = true;

        public CelestialObject(string celestialObjectDefName) {
            celestialObjectDef = Defs.Loader.celestialObjects[celestialObjectDefName];

            if (celestialObjectDef.floatingLabel != null) {
                GameObject gameObject = UnityEngine.Object.Instantiate(Assets.gameObjectWorldText);
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                _textComponent = gameObject.GetComponent<TMPro.TextMeshPro>();
            }
            if (celestialObjectDef.trail != null) {
                GameObject gameObject = UnityEngine.Object.Instantiate(Assets.gameObjectWorldText);
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                gameObject.GetComponent<TMPro.TextMeshPro>().enabled = false;
                _trailComponent = gameObject.AddComponent<TrailRenderer>();
            }
        }

        ~CelestialObject() {
            if (_textComponent != null) UnityEngine.Object.Destroy(_textComponent.gameObject);
            if (_trailComponent != null) UnityEngine.Object.Destroy(_trailComponent.gameObject);
        }

        public virtual void Randomize() {
            Init();
        }

        public virtual void Init(int? seed = null, Vector3? currentPosition = null) {
            _seed = Rand.Int;
            _rand = new Functionality.Random(_seed);

            Defs.NamePack namePack = Defs.Loader.namePacks[celestialObjectDef.namePackDefName];
            name = $"{_rand.GetElement(namePack.prefix)}{_rand.GetElement(namePack.postfix)}";

            float size = _rand.GetValueBetween(celestialObjectDef.sizeBetween);
            _size = new Vector3(size, size, size);
            _orbitSpeed = _rand.GetValueBetween(celestialObjectDef.orbitSpeedBetween);
            _period = (int) (36000.0f + (6000.0f * (_rand.GetFloat() - 0.5f)));
            _timeOffset = _rand.GetValueBetween(new Vector2Int(0, _period));
            _orbitPath = celestialObjectDef.orbitPath;
            _orbitSpread = celestialObjectDef.orbitSpread;
            _orbitPosition = new Vector3 {
                x = _orbitPath.x + (float) ((Rand.Value - 0.5f) * (_orbitPath.x * _orbitSpread.x)),
                y = _rand.GetValueBetween(new Vector2(Math.Abs(_orbitPath.y) * -1, Math.Abs(_orbitPath.y))),
                z = _orbitPath.z + (float) ((Rand.Value - 0.5f) * (_orbitPath.z * _orbitSpread.z))
            };
            switch (celestialObjectDef.orbitDirection) {
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
            _axialRotationSpeed = _rand.GetValueBetween(celestialObjectDef.axialRotationSpeedBetween);
            
            if (currentPosition == null) {
                _currentPosition = _orbitPosition;
                UpdatePosition(tick: 0);
            } else _currentPosition = (Vector3) currentPosition;

            if (celestialObjectDef.shape != null) {
                _GenerateShape();
            } else if (celestialObjectDef.icon != null) {

            } else {
                Logger.print(
                    Logger.Importance.Error,
                    text: $"Celestial object def '{celestialObjectDef.defName}' doesn't contain either a Icon or Shape specification",
                    prefix: Style.name_prefix
                );
                return;
            }

            if (_textComponent != null) _GenerateLabel();
            if (_trailComponent != null) _GenerateTrail();
        }

        public virtual void WorldSceneActivated() {

        }

        public virtual void WorldSceneDeactivated() {

        }

        public virtual void Update(int tick, Vector3 cameraPosition, Vector3 center) {
            CheckHideTextComponent(cameraPosition, center);
            UpdateRotation(tick, center);
            UpdateTransformationMatrix();
        }

        public virtual void UpdateSingleThread(Vector3 cameraPosition, float altitudePercent, TimeSpeed speed) {
            UpdateTextComponentTransformationMatrix(cameraPosition, altitudePercent);
            UpdateTrailComponentTransformationMatrix(speed);
        }

        public virtual void UpdateWhenUnpaused(int tick) {
            UpdatePosition(tick);
        }

        public virtual void UpdateWhenCameraMoved() { }

        public virtual void UpdatePosition(int tick) {
            float time = _orbitSpeed * _orbitDirection * tick + _timeOffset;
            float angularFrequencyTime = 6.28f / _period * time;
            float yOffset = Math.Abs(_orbitPosition.y) / 2;
            _currentPosition.x = (_orbitPosition.x - yOffset) * (float) Math.Cos(angularFrequencyTime);
            _currentPosition.z = (_orbitPosition.z - yOffset) * (float) Math.Sin(angularFrequencyTime);
        }

        public virtual void UpdateRotation(int tick, Vector3 center) {
            Vector3 towards_camera = Vector3.Cross(center, Vector3.up);
            _billboardingRotation = Quaternion.LookRotation(towards_camera, center);

            _axialRotation = Quaternion.Euler(0.5f * _axialRotationSpeed * tick * _orbitDirection * -1, _axialRotationSpeed * tick * _orbitDirection * -1, 0);

            _rotation = _axialRotation;
        }

        public virtual void UpdateTransformationMatrix() {
            _transformationMatrix.SetTRS(_currentPosition, _rotation, _size);
            // update real position
            realPosition.x = _transformationMatrix.m03;
            realPosition.y = _transformationMatrix.m13;
            realPosition.z = _transformationMatrix.m23;
        }

        public virtual void UpdateTextComponentTransformationMatrix(Vector3 cameraPosition, float altitudePercent) {
            if (_textComponent == null) return;
            if (_textComponentBlock) SetTextComponentActive(false);
            var position = Vector3.MoveTowards(realPosition, cameraPosition, 50.0f);
            _textComponent.transform.localPosition = new Vector3(
                position.x,
                position.y - (_size.y + 1.0f + _extraSize),
                position.z
            );
            _textComponent.transform.localRotation = _billboardingRotation * Quaternion.Euler(90.0f, -90f, 0f);
        }

        public virtual void CheckHideTextComponent(Vector3 cameraPosition, Vector3 center) {
            if (_textComponent == null) return;
            bool tooClose = (Vector3.Distance(_textComponent.transform.localPosition, center) + (220.0f * _textComponentHideAtMinAltitude)) > Vector3.Distance(center, cameraPosition);
            bool tooFar = (Vector3.Distance(_textComponent.transform.localPosition, center) + (220.0f * _textComponentHideAtMaxAltitude)) < Vector3.Distance(center, cameraPosition);
            if (tooClose || tooFar) {
                if (!_textComponentBlock) _textComponentBlock = true;
            } else if (_textComponentBlock) _textComponentBlock = false;
        }

        public virtual void SetTextComponentActive(bool active) {
            if (_textComponent == null) return;
            if (_textComponentActive == active) return;
            _textComponent.gameObject.SetActive(active);
            _textComponentActive = active;
        }

        public virtual void UpdateTrailComponentTransformationMatrix(TimeSpeed speed) {
            if (_trailComponent == null) return;
            if (!_trailComponentActive) return;
            _trailComponent.transform.set_position_Injected(ref realPosition);
            if (speed != _prevSpeed) {
                _prevSpeed = speed;
                float speedValue = (float) Math.Pow(3.0, (double) speed - 1.0);
                if (speedValue <= 0) {
                    _trailComponent.time = 0.0f;
                } else _trailComponent.time = _trailLength / speedValue;
            }
            if (_firstRender) {
                _firstRender = false;
                _trailComponent.Clear();
            }
        }

        public virtual void SetTrailComponentActive(bool active) {
            if (_trailComponent == null) return;
            if (_trailComponentActive == active) return;
            _trailComponent.gameObject.SetActive(active);
            _trailComponentActive = active;
            _trailComponent.enabled = _trailComponentActive;
            _trailComponent.Clear();
        }

        public virtual void ClearTrail() {
            if (_trailComponent == null) return;
            _trailComponent.Clear();
        }

        public virtual void Render() {
            _shape?.Render(_transformationMatrix);
            if (!_textComponentBlock) SetTextComponentActive(true);
            SetTrailComponentActive(true);
        }

        public virtual Vector3 GetOrbitAroundPosition() {
            return _orbitTarget?.realPosition ?? Vector3.zero;
        }

        public virtual void SetOrbitTarget(CelestialObject target) {
            _orbitTarget = target;
        }

        private void _GenerateLabel() {
            if (celestialObjectDef.floatingLabel.overwriteText != null) {
                _textComponent.text = celestialObjectDef.floatingLabel.overwriteText;
            } else _textComponent.text = name;
            _textComponentHideAtMinAltitude = celestialObjectDef.floatingLabel.hideAtMinAltitude;
            _textComponentHideAtMaxAltitude = celestialObjectDef.floatingLabel.hideAtMaxAltitude;
            _textComponent.color = celestialObjectDef.floatingLabel.color;
            _textComponent.font = TMPro.TMP_FontAsset.CreateFontAsset((Font) Resources.Load(celestialObjectDef.floatingLabel.fontPath));
            _textComponent.fontSize = celestialObjectDef.floatingLabel.fontSize;
            _textComponent.outlineColor = celestialObjectDef.floatingLabel.outlineColor;
            _textComponent.outlineWidth = celestialObjectDef.floatingLabel.outlineWidth;
            _textComponent.overflowMode = TMPro.TextOverflowModes.Overflow;
            foreach (Material sharedMaterial in _textComponent.GetComponent<MeshRenderer>().sharedMaterials) {
                sharedMaterial.renderQueue = RimWorld.Planet.WorldMaterials.FeatureNameRenderQueue;
            }
            SetTextComponentActive(false);
        }

        private void _GenerateTrail() {
            _prevSpeed = TimeSpeed.Paused;
            _trailLength = celestialObjectDef.trail.length;
            _trailComponent.startWidth = _size.x * celestialObjectDef.trail.width;
            _trailComponent.endWidth = 0.0f;
            _trailComponent.time = 0.0f;
            _trailComponent.material = Assets.materials[celestialObjectDef.trail.materialDefName];
            Color color = celestialObjectDef.trail.color;
            _trailComponent.startColor = new Color(color.r, color.g, color.b, celestialObjectDef.trail.transparency);
            _trailComponent.endColor = new Color(color.r, color.g, color.b, 0.0f);
            foreach (Material sharedMaterial in _trailComponent.sharedMaterials) {
                sharedMaterial.renderQueue = _trailComponent.material.renderQueue;
            }
        }

        private void _GenerateShape() {
            _shape = new Shape(_seed);

            foreach (Defs.Mesh mesh in celestialObjectDef.shape.meshes) {
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

            _extraSize = _shape.highestElevation;
        }
    }
}
