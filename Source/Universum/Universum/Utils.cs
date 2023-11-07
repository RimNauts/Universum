using UnityEngine;

namespace Universum {
    public static class Utils {
        public static Quaternion billboardRotation() {
            return Quaternion.LookRotation(Game.MainLoop.instance.cameraForward, Vector3.up); ;
        }
    }
}
