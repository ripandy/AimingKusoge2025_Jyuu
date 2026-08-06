using System;
using YukiQuest.SOAR;
using R3;
using Soar.Variables;
using UnityEngine;

namespace YukiQuest.Gameplay
{
    /// <summary>
    /// Keeps the bee inside the stage. The sides are hard walls — the old left/right wrapping is
    /// gone now that the stage scrolls. Flying out of the top returns the bee to the nest, which is
    /// the closest thing this chapter has to a penalty: carried pollen is kept.
    /// </summary>
    public class BoundaryHandler : MonoBehaviour
    {
        [SerializeField] private StageBoundsVariable stageBounds;
        [SerializeField] private Variable<Transform> hivePoint;

        private BeeMoveController beeMoveController;
        private Rigidbody2D beeBody;
        private IDisposable subscription;

        private void Start()
        {
            beeMoveController = GetComponent<BeeMoveController>();
            beeBody = GetComponent<Rigidbody2D>();

            // On the physics clock, not the render clock. The bee is an interpolated Rigidbody2D, so
            // its transform between fixed steps is a *visual* guess — clamping that and writing it
            // back would feed the guess into the simulation and make the bee shudder along a wall.
            subscription = Observable.EveryUpdate(UnityFrameProvider.FixedUpdate, destroyCancellationToken)
                .Subscribe(_ => HandleBoundaries());
        }

        private void HandleBoundaries()
        {
            if (stageBounds == null) return;

            var bounds = stageBounds.Value;
            if (bounds.width <= 0f || bounds.height <= 0f) return;

            var position = beeBody != null ? beeBody.position : (Vector2)transform.position;

            if (position.y > bounds.yMax)
            {
                ReturnToHive();
                return;
            }

            var clampedX = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);

            // Only write when the clamp actually bites. An unconditional assignment every step
            // re-syncs the physics transform and discards the interpolation state, which reads as
            // jitter even when the bee is nowhere near an edge.
            if (Mathf.Approximately(clampedX, position.x)) return;

            MoveTo(new Vector2(clampedX, position.y));
        }

        private void ReturnToHive()
        {
            if (hivePoint == null || hivePoint.Value == null) return;

            MoveTo(hivePoint.Value.position);

            if (beeMoveController != null)
                beeMoveController.ResetMomentum();
        }

        private void MoveTo(Vector2 position)
        {
            if (beeBody != null) beeBody.position = position;
            else transform.position = position;
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
        }
    }
}
