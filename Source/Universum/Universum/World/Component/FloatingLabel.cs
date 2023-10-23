using UnityEngine;

namespace Universum.World.Component {
    public class FloatingLabel : ObjectComponent {
        private readonly TMPro.TextMeshPro _textComponent;

        public FloatingLabel(CelestialObject celestialObject, Defs.Component def) : base(celestialObject, def) {
            _textComponent = _gameObject.GetComponent<TMPro.TextMeshPro>();

            _textComponent.text = def.overwriteText ?? celestialObject.name;
            _textComponent.color = def.color;
            _textComponent.fontSize = def.fontSize;
            _textComponent.outlineColor = def.outlineColor;
            _textComponent.outlineWidth = def.outlineWidth;
            _textComponent.overflowMode = TMPro.TextOverflowModes.Overflow;
            foreach (Material sharedMaterial in _textComponent.GetComponent<MeshRenderer>().sharedMaterials) {
                sharedMaterial.renderQueue = RimWorld.Planet.WorldMaterials.FeatureNameRenderQueue;
            }

            SetActive(false);
        }

        public override void UpdateInfo() {
            base.UpdateInfo();
            _textComponent.text = _def.overwriteText ?? _celestialObject.name;
        }

        public override void Update() {
            base.Update();
            hide();
        }

        public override void UpdatePosition() {
            Vector3 directionFromObjectToCamera = Game.MainLoop.instance.cameraPosition - _celestialObject.transformedPosition;
            directionFromObjectToCamera.Normalize();

            _position = _celestialObject.transformedPosition + directionFromObjectToCamera * (_celestialObject.scale.y + _celestialObject.extraScale) * 1.2f;
            _position -= Game.MainLoop.instance.cameraUp * (_celestialObject.scale.y + _celestialObject.extraScale) * 1.2f;
        }

        public override void UpdateRotation() {
            Vector3 directionFromLabelToCamera = Game.MainLoop.instance.cameraPosition - _position;
            _rotation = Quaternion.LookRotation(-directionFromLabelToCamera);
        }

        public override void UpdateTransformationMatrix() {
            _textComponent.transform.localPosition = _position;
            _textComponent.transform.rotation = _rotation;
        }

        private void hide() {
            float distanceFromCamera = Vector3.Distance(_position, Game.MainLoop.instance.cameraPosition);

            bool tooClose = distanceFromCamera < _hideAtMinAltitude;
            bool tooFar = distanceFromCamera > _hideAtMaxAltitude;
            bool outsideRange = tooClose || tooFar;

            SetBlock(outsideRange);
        }
    }
}
