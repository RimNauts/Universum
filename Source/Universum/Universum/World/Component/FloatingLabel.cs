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

        public override void Update() {
            base.Update();
            hide();
        }

        public override void UpdatePosition() {
            _position = Vector3.MoveTowards(_celestialObject.transformedPosition, Game.MainLoop.instance.cameraPosition, 50.0f);
            _position += _offset;
            _position.y -= _celestialObject.scale.y + _celestialObject.extraScale + 1.0f;
        }

        public override void UpdateRotation() {
            _rotation = _celestialObject.billboardRotation * Quaternion.Euler(90.0f, -90f, 0f);
        }

        public override void UpdateTransformationMatrix() {
            _textComponent.transform.localPosition = _position;
            _textComponent.transform.rotation = _rotation;
        }

        private void hide() {
            float distanceFromCamera = Vector3.Distance(_position, Game.MainLoop.instance.cameraPosition);

            bool tooClose = distanceFromCamera < _hideAtMinAltitude;
            bool tooFar = distanceFromCamera > _hideAtMaxAltitude;

            if (tooClose || tooFar) {
                SetBlock(true);
            } else SetBlock(false);
        }
    }
}
