using System;
using System.Linq;
using Domain;
using YukiQuest.SOAR;
using R3;
using Soar.Variables;
using UnityEngine;

namespace YukiQuest.Gameplay
{
    /// <summary>
    /// Player control for the bee. Gravity-free: the bee holds whatever height the player flies it
    /// to. Velocity is driven directly rather than through forces, so the handling stays
    /// predictable for a young player.
    /// </summary>
    public class BeeMoveController : MonoBehaviour, IBeeMoveController
    {
        [SerializeField] private BeeList beeList;
        [SerializeField] private Variable<Vector2> moveInput;
        [SerializeField] private Variable<bool> flapInput;
        [SerializeField] private Transform baseTransform;
        [SerializeField] private Rigidbody2D beeBody;
        [SerializeField] private Animator wingAnimator;

        [Header("Output")]
        [Tooltip("Set to this bee's transform so the camera can follow it.")]
        [SerializeField] private Variable<Transform> playerBeeTransform;

        [Header("Feel")]
        [Tooltip("How long the bee takes to reach full speed, and to coast to a stop.")]
        [SerializeField] private float moveSmoothTime = 0.12f;
        [Tooltip("How quickly a flap or a bounce fades out, in units per second squared.")]
        [SerializeField] private float impulseDecay = 2.5f;
        [Tooltip("Upward kick when bumping the ground. Deliberately soft.")]
        [SerializeField] private float bounceImpulse = 1.5f;
        [SerializeField] private float flapAnimSpeed = 3f;
        [SerializeField] private float flapAnimDuration = 0.35f;
        [Tooltip("How long the bee screws its eyes shut while flapping.")]
        [SerializeField] private float flapBlinkDuration = 0.15f;

        private Rigidbody2D BeeBody => beeBody ??= GetComponent<Rigidbody2D>();

        private int beeId;
        private Vector3 defaultBeeScale;
        private BeePresenter beePresenter;

        // Intentional movement, and the transient kicks layered on top of it (flap, ground bounce).
        // They are tracked separately so a kick is not immediately smoothed away by the movement.
        private Vector2 moveVelocity;
        private Vector2 moveAcceleration;
        private Vector2 impulseVelocity;

        private float flurryTimer;

        private IDisposable subscriptions;

        public void Initialize(int id)
        {
            beeId = id;

            defaultBeeScale = baseTransform.localScale;
            beePresenter = GetComponent<BeePresenter>();

            // No gravity and no tumbling; the prefab values must not fight this.
            BeeBody.gravityScale = 0f;
            BeeBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            BeeBody.rotation = 0f;

            ResetMomentum();

            if (playerBeeTransform != null)
                playerBeeTransform.Value = transform;

            subscriptions?.Dispose();
            var s1 = Observable
                .EveryUpdate(UnityFrameProvider.FixedUpdate, destroyCancellationToken)
                .Subscribe(_ => Tick());
            var s2 = flapInput.AsObservable().Subscribe(FlapBee);
            subscriptions = Disposable.Combine(s1, s2);
        }

        /// <summary>
        /// Drops all momentum. Used when the bee is returned to the hive, so it does not carry its
        /// old velocity into the respawn.
        /// </summary>
        public void ResetMomentum()
        {
            moveVelocity = Vector2.zero;
            moveAcceleration = Vector2.zero;
            impulseVelocity = Vector2.zero;
            BeeBody.linearVelocity = Vector2.zero;
        }

        private void Tick()
        {
            var input = moveInput.Value;

            moveVelocity = Vector2.SmoothDamp(
                moveVelocity, input * beeList[beeId].MoveSpeed, ref moveAcceleration, moveSmoothTime);
            impulseVelocity = Vector2.MoveTowards(
                impulseVelocity, Vector2.zero, impulseDecay * Time.fixedDeltaTime);

            BeeBody.linearVelocity = moveVelocity + impulseVelocity;

            UpdateFacing(input.x);
            UpdateWingSpeed();
        }

        private void UpdateFacing(float horizontal)
        {
            if (Mathf.Approximately(horizontal, 0f)) return;

            var scale = defaultBeeScale;
            scale.x = Mathf.Abs(scale.x) * (horizontal < 0 ? 1 : -1);
            baseTransform.localScale = scale;
        }

        private void FlapBee(bool isFlap)
        {
            if (!isFlap) return;

            impulseVelocity += Vector2.up * beeList[beeId].FlapForce;
            flurryTimer = flapAnimDuration;

            if (beePresenter != null)
                beePresenter.Blink(flapBlinkDuration);
        }

        private void UpdateWingSpeed()
        {
            if (wingAnimator == null || flurryTimer <= 0f) return;

            flurryTimer -= Time.fixedDeltaTime;
            wingAnimator.speed = flurryTimer > 0f
                ? Mathf.Lerp(1f, flapAnimSpeed, flurryTimer / flapAnimDuration)
                : 1f;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.gameObject.CompareTag("Bounds")) return;

            var normal = other.contacts.First().normal;

            // A bounce REPLACES the current kick rather than adding to it. Holding the bee into the
            // ground re-triggers this every time it settles back onto the collider, and those
            // impulses used to stack faster than impulseDecay could bleed them off — enough to
            // launch the bee off the top of the stage.
            impulseVelocity = normal * bounceImpulse;
        }

        private void OnDestroy()
        {
            subscriptions?.Dispose();

            if (playerBeeTransform != null && playerBeeTransform.Value == transform)
                playerBeeTransform.Value = null;
        }
    }
}
