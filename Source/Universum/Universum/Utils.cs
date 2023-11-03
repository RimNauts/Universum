using UnityEngine;

namespace Universum {
    public static class Utils {
        public static Quaternion billboardRotation(Vector3 position, Vector3 targetPosition, Quaternion? correctionRotation = null) {
            // direction from the sprite to the target
            Vector3 toTarget = (targetPosition - position).normalized;
            Vector3 cameraForward = -Game.MainLoop.instance.cameraPosition.normalized;
            //sprite's forward direction should be orthogonal to both the cameras forward and world up direction
            Vector3 spriteForward = Vector3.Cross(cameraForward, Vector3.up).normalized;
            // calculate the billboard rotation
            Quaternion rotation = Quaternion.LookRotation(spriteForward, toTarget);
            // add correction rotation if needed
            if (correctionRotation != null) rotation *= (Quaternion) correctionRotation;

            return rotation;
        }
    }
}
