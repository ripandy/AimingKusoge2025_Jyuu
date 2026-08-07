using YukiQuest.SOAR;
using UnityEngine;

namespace YukiQuest.Gameplay
{
    /// <summary>
    /// Publishes this chapter's playable extent into the shared <see cref="StageBoundsVariable"/>.
    /// Each chapter scene carries its own instance, so the Core camera never needs to know which
    /// chapter is loaded.
    /// </summary>
    public class StageBoundsPublisher : MonoBehaviour
    {
        [SerializeField] private StageBoundsVariable stageBounds;

        [Header("Extent")]
        [SerializeField] private float left = -20f;
        [SerializeField] private float right = 20f;
        [SerializeField] private float bottom = -6f;
        [SerializeField] private float top = 8f;

        private Rect Bounds => Rect.MinMaxRect(left, bottom, right, top);

        private void Awake()
        {
            if (stageBounds == null)
            {
                Debug.LogError($"[{GetType().Name}][{name}] No StageBoundsVariable assigned.");
                return;
            }

            stageBounds.Value = Bounds;
        }

        private void OnDrawGizmos()
        {
            var bounds = Bounds;
            Gizmos.color = Color.cyan;

            var bottomLeft = new Vector3(bounds.xMin, bounds.yMin);
            var bottomRight = new Vector3(bounds.xMax, bounds.yMin);
            var topRight = new Vector3(bounds.xMax, bounds.yMax);
            var topLeft = new Vector3(bounds.xMin, bounds.yMax);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}
