using UnityEngine;

namespace Universum.World.Objects {
    internal class CrashingAsteroid : CelestialObject {
        private double _baseOrbitRadius;
        private double _radiusChangePerTick;
        private double _targetOrbitRadius;

        public CrashingAsteroid(string celestialObjectDefName) : base(celestialObjectDefName) { }

        public override void Init(int? seed = null, Vector3? position = null, int? deathTick = null) {
            base.Init(seed, position, deathTick);

            _baseOrbitRadius = _orbitRadius;
            double minOrbitRadius = 0.0;
            double maxOrbitRadius = _baseOrbitRadius * 2;

            _targetOrbitRadius = _rand.GetBool() ? minOrbitRadius : maxOrbitRadius;

            double minSpeed = def.speedPercentageBetween[0];
            double maxSpeed = def.speedPercentageBetween[1];

            double normalizedSpeed = (speed - minSpeed) / (maxSpeed - minSpeed);

            double minRadiusChange = 0.02;
            double maxRadiusChange = 0.05;
            _radiusChangePerTick = (1 - normalizedSpeed) * minRadiusChange + normalizedSpeed * maxRadiusChange;
        }

        public override void UpdatePosition(int tick) {
            _AdjustOrbitRadius();

            base.UpdatePosition(tick);
        }

        private void _AdjustOrbitRadius() {
            double direction = (_orbitRadius < _targetOrbitRadius) ? 1.0 : -1.0;

            _orbitRadius += direction * _radiusChangePerTick;

            if ((direction == 1.0 && _orbitRadius > _targetOrbitRadius) ||
                (direction == -1.0 && _orbitRadius < _targetOrbitRadius)) {
                _ResetOrbitRadiusAndClearComponents();
            }
        }

        private void _ResetOrbitRadiusAndClearComponents() {
            _orbitRadius = _baseOrbitRadius;
            for (int i = 0; i < _components.Length; i++) _components[i].Clear();
        }
    }
}
