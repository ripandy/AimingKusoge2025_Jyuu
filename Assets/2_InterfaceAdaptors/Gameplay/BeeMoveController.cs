using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Domain;
using Kusoge.SOAR;
using R3;
using Soar.Variables;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kusoge.Gameplay
{
    public class BeeMoveController : MonoBehaviour, IBeeMoveController
    {
        [SerializeField] private GameJsonableVariable gameData;
        [SerializeField] private BeeList beeList;
        [SerializeField] private Variable<Vector2> moveInput;
        [SerializeField] private Variable<bool> flapInput;
        [SerializeField] private Transform baseTransform;
        [SerializeField] private Rigidbody2D beeBody;
        
        private Rigidbody2D BeeBody => beeBody ??= GetComponent<Rigidbody2D>();
        
        private int beeId;

        private float moveForce = 5f;
        private Vector2 flapForce = new(0, 5f);
        private Vector3 defaultBeeScale;
        private bool isMoving;
        private bool isRecoveringRotation;
        
        private IDisposable subscriptions;
        
        public void Initialize(int id)
        {
            beeId = id;
            
            defaultBeeScale = baseTransform.localScale;
            
            UpdateBeePhysics(beeList[beeId]);
            
            // subscribe to bee updates
            subscriptions?.Dispose();
            var s1 = Observable
                .EveryUpdate(UnityFrameProvider.FixedUpdate, destroyCancellationToken)
                .Subscribe(_ =>
                {
                    isMoving = moveInput.Value.magnitude > 0.01f;
                    MoveBee(moveInput.Value, ForceMode2D.Force);
                    RecoverRotationAttempt().Forget();
                });
            var s2 = flapInput.AsObservable().Subscribe(FlapBee);
            var s3 = beeList.SubscribeToValues(beeId, UpdateBeePhysics);
            subscriptions = Disposable.Combine(s1, s2, s3);
            
            // initial launch
            var launchVector = new Vector2(-1, Random.Range(-0.3f, 0.3f));
            MoveBee(launchVector * beeList[beeId].FlapForce, ForceMode2D.Impulse);
            IdleFloating().Forget();
            
            void UpdateBeePhysics(Bee bee)
            {
                moveForce = bee.MoveForce;
                flapForce = new Vector2(0f, bee.FlapForce);
                BeeBody.mass = bee.BaseWeight + bee.Nectar * gameData.Value.NectarWeight;
                
                var scale = defaultBeeScale * BeeBody.mass;
                scale.x *= baseTransform.localScale.x < 0 ? -1 : 1;
                baseTransform.localScale = scale;
            }
        }

        private void MoveBee(Vector2 moveVector, ForceMode2D forceMode)
        {
            var force = moveVector * moveForce;
            BeeBody.AddForce(force, forceMode);
            
            if (moveVector.x == 0) return;
            
            var scale = baseTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * (moveVector.x < 0 ? 1 : -1);
            baseTransform.localScale = scale;
        }

        private void FlapBee(bool isFlap)
        {
            if (!isFlap) return;
            MoveBee(flapForce, ForceMode2D.Impulse);
        }

        private async UniTaskVoid RecoverRotationAttempt()
        {
            if (isRecoveringRotation) return;

            // Check if the bee is significantly tilted and its angular velocity is low.
            var isTilted = Mathf.Abs(baseTransform.rotation.eulerAngles.z) > 45f;
            var isSlowingDown = Mathf.Abs(BeeBody.angularVelocity) < 5f;

            if (!isTilted || !isSlowingDown) return;

            isRecoveringRotation = true;

            const float duration = 1f;
            var elapsedTime = 0f;
            var startRotation = baseTransform.rotation;
            
            // Generate a random target Z rotation between -15 and 15 degrees
            var randomZ = Random.Range(-15f, 15f);
            var targetRotation = Quaternion.Euler(0, 0, randomZ);

            while (elapsedTime < duration)
            {
                // Stop if physics causes significant rotation again
                if (Mathf.Abs(BeeBody.angularVelocity) > 30f)
                {
                    isRecoveringRotation = false;
                    return;
                }

                elapsedTime += Time.deltaTime;
                var t = elapsedTime / duration;
                // Use MoveRotation for smooth, physics-friendly rotation
                BeeBody.MoveRotation(Quaternion.Slerp(startRotation, targetRotation, t));
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
            }

            BeeBody.MoveRotation(targetRotation);
            isRecoveringRotation = false;
        }

        private async UniTaskVoid IdleFloating()
        {
            while (!destroyCancellationToken.IsCancellationRequested)
            {
                if (!isMoving)
                {
                    var floatForce = Vector2.up * beeList[beeId].BaseWeight * BeeBody.gravityScale;
                    MoveBee(floatForce, ForceMode2D.Impulse);
                }

                const float rndRange = 0.4f;
                var delay = 1f + rndRange * 0.5f - Random.value * rndRange;
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
            }
        }
        
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.gameObject.CompareTag("Bounds")) return;
            var normal = other.contacts.First().normal;
            MoveBee(normal * BeeBody.mass * 0.5f, ForceMode2D.Impulse);
        }

        private void OnDestroy()
        {
            subscriptions?.Dispose();
        }
    }
}
