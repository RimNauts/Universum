using System;
using UnityEngine;
using Verse;

namespace Universum.World.Component {
    public class Trail : ObjectComponent {
        private readonly TrailRenderer _trailComponent;

        private TimeSpeed _prevGameSpeed;
        private readonly float _trailLength;
        private float _speed;

        public Trail(CelestialObject celestialObject, Defs.Component def) : base(celestialObject, def) {
            _gameObject.GetComponent<TMPro.TextMeshPro>().enabled = false;
            _trailComponent = _gameObject.AddComponent<TrailRenderer>();

            _prevGameSpeed = TimeSpeed.Paused;
            _trailLength = def.trailLength;
            _trailComponent.startWidth = _celestialObject.scale.x * def.trailWidth;
            _trailComponent.endWidth = 0.0f;
            _trailComponent.time = 0.0f;
            _trailComponent.material = Assets.materials[def.materialDefName];
            Color color = def.color;
            _trailComponent.startColor = new Color(color.r, color.g, color.b, def.trailTransparency);
            _trailComponent.endColor = new Color(color.r, color.g, color.b, 0.0f);
            foreach (Material sharedMaterial in _trailComponent.sharedMaterials) {
                sharedMaterial.renderQueue = _trailComponent.material.renderQueue;
            }

            SetActive(false);
        }

        public override void Clear() {
            base.Clear();
            _trailComponent.Clear();
        }

        public override void OnWorldSceneActivated() {
            base.OnWorldSceneActivated();
            _trailComponent.Clear();
        }

        public override void OnWorldSceneDeactivated() {
            base.OnWorldSceneDeactivated();
            _trailComponent.Clear();
        }

        public override void Update() {
            SetBlock(!Utilities.Cache.allowed_utility("universum.trails"));

            base.Update();
            TimeSpeed currentSpeed = Game.MainLoop.instance.timeSpeed;
            if (currentSpeed != _prevGameSpeed) {
                _prevGameSpeed = currentSpeed;
                _speed = (float) Math.Pow(3.0, (double) currentSpeed - 1.0);
            }
        }

        public override void UpdateTransformationMatrix() {
            _trailComponent.transform.position = _position;
            if (_speed <= 0) {
                _trailComponent.time = 0.0f;
            } else _trailComponent.time = _trailLength / _speed;
        }

        public override void SetActive(bool active) {
            if (_active != active) _trailComponent.Clear();
            base.SetActive(active);
        }
    }
}
