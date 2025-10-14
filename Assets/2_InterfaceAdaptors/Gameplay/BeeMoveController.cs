using System;
using System.Linq;
using Domain;
using R3;
using Soar.Variables;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Kusoge.Gameplay
{
    public class BeeMoveController : MonoBehaviour, IBeeMoveController
    {
        [SerializeField] private Variable<Vector2> moveInput;
        [SerializeField] private Transform baseTransform;

        private Bee bee;
        
        private Rigidbody2D beeBody;
        private Rigidbody2D BeeBody => beeBody ??= GetComponent<Rigidbody2D>();

        private const float LaunchForce = 100f;
        
        private float defaultScaleX;
        
        private IDisposable moveInputSubscription;

        private void Start()
        {
            Assert.IsFalse(BeeBody == null, $"[{GetType().Name}][{name}] Bee has no Rigidbody2D assigned.");
            
            moveInputSubscription = Observable
                .EveryUpdate(destroyCancellationToken)
                .Subscribe(_ => MoveBee(moveInput.Value));
        }
        
        public void Initialize(Bee beeRef)
        {
            bee = beeRef;
            defaultScaleX = baseTransform.localScale.x;
            var launchVector = new Vector2(-1, Random.Range(-0.3f, 0.3f));
            MoveBee(launchVector * LaunchForce);
        }

        private void MoveBee(Vector2 moveVector)
        {
            BeeBody.AddForce(moveVector * bee.MoveSpeed);
            
            if (moveVector.x == 0) return;
            
            var scale = baseTransform.localScale;
            scale.x = defaultScaleX * (moveVector.x < 0 ? 1 : -1);
            baseTransform.localScale = scale;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.gameObject.CompareTag("Bounds")) return;
            var normal = other.contacts.First().normal;
            MoveBee(normal * LaunchForce);
        }

        private void OnDestroy()
        {
            moveInputSubscription?.Dispose();
        }
    }
}
