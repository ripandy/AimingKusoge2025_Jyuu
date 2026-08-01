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
        private IDisposable subscription;

        private void Start()
        {
            beeMoveController = GetComponent<BeeMoveController>();
            subscription = Observable.EveryUpdate(destroyCancellationToken)
                .Subscribe(_ => HandleBoundaries());
        }

        private void HandleBoundaries()
        {
            if (stageBounds == null) return;

            var bounds = stageBounds.Value;
            if (bounds.width <= 0f || bounds.height <= 0f) return;

            var position = transform.position;

            if (position.y > bounds.yMax)
            {
                ReturnToHive();
                return;
            }

            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);
            transform.position = position;
        }

        private void ReturnToHive()
        {
            if (hivePoint == null || hivePoint.Value == null) return;

            transform.position = hivePoint.Value.position;

            if (beeMoveController != null)
                beeMoveController.ResetMomentum();
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
        }
    }
}
