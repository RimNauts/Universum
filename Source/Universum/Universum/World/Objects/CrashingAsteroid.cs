using UnityEngine;

namespace Universum.World.Objects {
    internal class CrashingAsteroid : CelestialObject {
        private float _baseOrbitRadius;
        private float _radiusChangePerTick;
        private float _targetOrbitRadius;

        public CrashingAsteroid(string celestialObjectDefName) : base(celestialObjectDefName) { }

        public override void Init(int? seed = null, Vector3? position = null, int? deathTick = null) {
            base.Init(seed, position, deathTick);

            _baseOrbitRadius = _orbitRadius;
            float minOrbitRadius = 0.0f;
            float maxOrbitRadius = _baseOrbitRadius * 2;

            _targetOrbitRadius = _rand.GetBool() ? minOrbitRadius : maxOrbitRadius;

            float minSpeed = def.speedPercentageBetween[0];
            float maxSpeed = def.speedPercentageBetween[1];

            float normalizedSpeed = (speed - minSpeed) / (maxSpeed - minSpeed);

            float minRadiusChange = 0.02f;
            float maxRadiusChange = 0.05f;
            _radiusChangePerTick = (1 - normalizedSpeed) * minRadiusChange + normalizedSpeed * maxRadiusChange;

            if (_orbitRadius < _targetOrbitRadius) _radiusChangePerTick *= 2;
        }

        public override void UpdatePosition(int tick) {
            _AdjustOrbitRadius();

            base.UpdatePosition(tick);
        }

        private void _AdjustOrbitRadius() {
            float direction = (_orbitRadius < _targetOrbitRadius) ? 1.0f : -1.0f;

            _orbitRadius += direction * _radiusChangePerTick;

            if ((direction == 1.0f && _orbitRadius > _targetOrbitRadius) ||
                (direction == -1.0f && _orbitRadius < _targetOrbitRadius)) {
                _ResetOrbitRadiusAndClearComponents();
            }
        }

        private void _ResetOrbitRadiusAndClearComponents() {
            _orbitRadius = _baseOrbitRadius;
            for (int i = 0; i < _components.Length; i++) _components[i].Clear();
        }
    }
}
