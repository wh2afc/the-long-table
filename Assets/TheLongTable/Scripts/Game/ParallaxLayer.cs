using UnityEngine;

namespace LongTable.Game
{
    /// <summary>
    /// Moves a painted layer relative to the camera. follow = 1 stays fixed to the
    /// camera (sky), 0 is the playfield, negative values pass faster (foreground).
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        public float follow;
        public Transform cameraTransform;

        private float baseX;

        private void Start() => baseX = transform.position.x;

        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            var p = transform.position;
            p.x = baseX + cameraTransform.position.x * follow;
            transform.position = p;
        }
    }
}
