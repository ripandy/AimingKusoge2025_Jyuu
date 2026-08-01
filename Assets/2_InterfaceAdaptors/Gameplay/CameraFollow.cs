using YukiQuest.SOAR;
using Soar.Variables;
using UnityEngine;

namespace YukiQuest.Gameplay
{
    /// <summary>
    /// Follows the player character across the stage, clamped so the view never shows past the
    /// stage edges. Lives on the Core scene's camera, which outlives every chapter, so both the
    /// target and the extent arrive through SOAR variables rather than scene references.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Variable<Transform> target;
        [SerializeField] private StageBoundsVariable stageBounds;
        [SerializeField] private float smoothTime = 0.2f;
        [SerializeField] private Vector2 offset = Vector2.zero;

        private Camera followCamera;
        private Vector3 followVelocity;

        private void Awake()
        {
            followCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null || target.Value == null) return;

            var targetPosition = target.Value.position;
            var desired = new Vector3(
                targetPosition.x + offset.x,
                targetPosition.y + offset.y,
                transform.position.z);

            desired = ClampToStage(desired);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, smoothTime);
        }

        private Vector3 ClampToStage(Vector3 desired)
        {
            if (stageBounds == null) return desired;

            var bounds = stageBounds.Value;
            if (bounds.width <= 0f || bounds.height <= 0f) return desired;

            var halfHeight = followCamera.orthographicSize;
            var halfWidth = halfHeight * followCamera.aspect;

            // When the stage is narrower or shorter than the view, centre on it instead of
            // clamping to an inverted range.
            desired.x = bounds.width <= halfWidth * 2f
                ? bounds.center.x
                : Mathf.Clamp(desired.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);

            desired.y = bounds.height <= halfHeight * 2f
                ? bounds.center.y
                : Mathf.Clamp(desired.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight);

            return desired;
        }
    }
}
