using System;
using R3;
using Soar.Variables;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BeeMoveController : MonoBehaviour
    {
        [SerializeField] private Variable<Vector2> moveInput;
        [SerializeField] private Rigidbody2D beeBody;
        [SerializeField] private float moveSpeed = 1f;

        private float defaultScaleX;
        
        private IDisposable moveInputSubscription;

        private void Start()
        {
            if (beeBody == null)
            {
                Debug.LogError($"[BeeMoveController][{name}] Bee has no Rigidbody2D assigned.", this);
                return;
            }
            defaultScaleX = beeBody.transform.localScale.x;

            moveInputSubscription = Observable
                .EveryUpdate(destroyCancellationToken)
                .Subscribe(_ => MoveBee(moveInput.Value));
        }

        private void MoveBee(Vector2 moveVector)
        {
            if (beeBody == null) return;
            
            beeBody.AddForce(moveVector * moveSpeed);
            
            if (moveVector.x == 0) return;
            
            var scale = beeBody.transform.localScale;
            scale.x = defaultScaleX * (moveVector.x < 0 ? 1 : -1);
            beeBody.transform.localScale = scale;
        }
        
        private void OnDestroy()
        {
            moveInputSubscription?.Dispose();
        }
    }
}
