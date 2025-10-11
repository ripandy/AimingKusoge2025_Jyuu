using System;
using R3;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BoundaryHandler : MonoBehaviour
    {
        [SerializeField] private Vector2 horizontalBoundaries = new(-10f, 10f);
        
        private IDisposable subscription;

        private void Start()
        {
            subscription = Observable.EveryUpdate(destroyCancellationToken)
                .Subscribe(_ => HandleBoundaries());
        }

        private void HandleBoundaries()
        {
            var position = transform.position;

            if (position.x < horizontalBoundaries.x)
            {
                position.x = horizontalBoundaries.y;
            }
            else if (position.x > horizontalBoundaries.y)
            {
                position.x = horizontalBoundaries.x;
            }

            transform.position = position;
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
        }
    }
}