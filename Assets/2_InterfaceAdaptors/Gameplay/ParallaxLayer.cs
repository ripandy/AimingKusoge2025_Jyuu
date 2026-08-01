using UnityEngine;

namespace YukiQuest.Gameplay
{
    /// <summary>
    /// Offsets a background layer against the camera to fake depth. A factor of 0 leaves the layer
    /// pinned to the world (foreground); 1 pins it to the camera (infinitely distant sky).
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float horizontalFactor = 0.5f;
        [SerializeField, Range(0f, 1f)] private float verticalFactor = 0.2f;

        private Transform cameraTransform;
        private Vector3 startPosition;
        private Vector3 cameraStartPosition;

        private void Start()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning($"[{GetType().Name}][{name}] No main camera; parallax disabled.");
                enabled = false;
                return;
            }

            cameraTransform = mainCamera.transform;
            startPosition = transform.position;
            cameraStartPosition = cameraTransform.position;
        }

        private void LateUpdate()
        {
            var travel = cameraTransform.position - cameraStartPosition;

            transform.position = new Vector3(
                startPosition.x + travel.x * horizontalFactor,
                startPosition.y + travel.y * verticalFactor,
                startPosition.z);
        }
    }
}
