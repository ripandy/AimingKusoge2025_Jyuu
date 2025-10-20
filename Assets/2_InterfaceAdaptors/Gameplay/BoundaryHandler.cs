using System;
using R3;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BoundaryHandler : MonoBehaviour
    {
        [Header("Boundaries")]
        [SerializeField] private float left = -10f;
        [SerializeField] private float right = 10f;
        [SerializeField] private float top = 5f;
        [SerializeField] private float bottom = -5f;
        
        private IDisposable subscription;

        private void Start()
        {
            subscription = Observable.EveryUpdate(destroyCancellationToken)
                .Subscribe(_ => HandleBoundaries());
        }

        private void HandleBoundaries()
        {
            var position = transform.position;

            if (position.x < left)
            {
                position.x = right;
            }
            else if (position.x > right)
            {
                position.x = left;
            }

            // wrap top to bottom only. do not check bottom due to bounce.
            if (position.y > top)
            {
                position.y = bottom;
            }

            transform.position = position;
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
        }
    }
}