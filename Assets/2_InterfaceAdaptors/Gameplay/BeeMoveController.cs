using System;
using System.Linq;
using Domain;
using Kusoge.SOAR;
using R3;
using Soar.Variables;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Kusoge.Gameplay
{
    public class BeeMoveController : MonoBehaviour, IBeeMoveController
    {
        [SerializeField] private BeeList beeList;
        [SerializeField] private Variable<Vector2> moveInput;
        [SerializeField] private Transform baseTransform;
        
        private Rigidbody2D beeBody;
        private Rigidbody2D BeeBody => beeBody ??= GetComponent<Rigidbody2D>();

        private const float LaunchForce = 50f;
        
        private float defaultScaleX;
        
        private int beeId;
        private float MoveSpeed => beeId < beeList.Count
            ? beeList[beeId].MoveSpeed
            : 0f;
        
        private IDisposable moveInputSubscription;

        private void Start()
        {
            Assert.IsFalse(BeeBody == null, $"[{GetType().Name}][{name}] Bee has no Rigidbody2D assigned.");
            
            moveInputSubscription = Observable
                .EveryUpdate(UnityFrameProvider.FixedUpdate, destroyCancellationToken)
                .Subscribe(_ => MoveBee(moveInput.Value));
        }
        
        public void Initialize(int id)
        {
            beeId = id;
            defaultScaleX = baseTransform.localScale.x;
            var launchVector = new Vector2(-1, Random.Range(-0.3f, 0.3f));
            MoveBee(launchVector * LaunchForce);
        }

        private void MoveBee(Vector2 moveVector)
        {
            var force = moveVector * MoveSpeed;
            BeeBody.AddForce(force);
            
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
