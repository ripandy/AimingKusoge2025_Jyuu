using Soar.Variables;
using UnityEngine;
using Random = System.Random;

namespace YukiQuest.Gameplay
{
    /// <summary>
    /// A background bee. It wanders around an anchor point and bumps into the rest of the swarm, but
    /// it takes no input, harvests nothing and knows nothing about the game loop.
    /// </summary>
    /// <remarks>
    /// Physics setup deliberately mirrors <see cref="BeeMoveController"/> so the swarm carries the
    /// same weight as the player. What keeps it out of the player's way is the physics layer, not
    /// this component — see the DecorBee row of the Physics2D matrix.
    /// </remarks>
    public class DecorBeeController : MonoBehaviour
    {
        [SerializeField] private Transform baseTransform;
        [SerializeField] private Rigidbody2D beeBody;
        [SerializeField] private Animator wingAnimator;

        [Header("Feel")]
        [SerializeField] private float moveSpeed = 2f;
        [Tooltip("How long the bee takes to reach full speed, and to coast to a stop.")]
        [SerializeField] private float moveSmoothTime = 0.45f;
        [Tooltip("How far from its anchor the bee will drift, horizontally and vertically.")]
        [SerializeField] private Vector2 roamRadius = new(4f, 2f);
        [Tooltip("Seconds before the bee picks somewhere new, even if it never arrived.")]
        [SerializeField] private Vector2 repathInterval = new(1.5f, 4f);
        [Tooltip("How close counts as arrived.")]
        [SerializeField] private float arriveDistance = 0.35f;

        [Header("Bumping")]
        [Tooltip("How light this bee is against the player's mass of 1. Low mass is what makes the " +
                 "same collision throw the bee hard and barely move the player.")]
        [SerializeField] private float mass = 0.15f;
        [Tooltip("Kick applied on any collision, before the speed of the hit is added on.")]
        [SerializeField] private float bounceImpulse = 3f;
        [Tooltip("How much of the closing speed feeds into the kick. A graze and a hard hit should " +
                 "not look the same.")]
        [SerializeField] private float bounceSpeedScale = 0.8f;
        [SerializeField] private float maxBounceImpulse = 12f;
        [Tooltip("Units per second squared. Slower than the player's, so a bump carries further.")]
        [SerializeField] private float impulseDecay = 1.2f;

        [Header("Following")]
        [Tooltip("Set by the generator on the few bees that tag along with the player. Left empty, " +
                 "the bee wanders around a fixed spot instead.")]
        [SerializeField] private Variable<Transform> followTarget;
        [SerializeField] private Vector2 followOffset = new(0f, 1.5f);
        [Tooltip("Followers need to outrun the player's speed of 7 to catch up after falling behind.")]
        [SerializeField] private float followSpeed = 9f;
        [Tooltip("How loosely a follower orbits. Tight enough to read as company, loose enough not " +
                 "to sit on top of the player while they are harvesting.")]
        [SerializeField] private Vector2 followRoamRadius = new(2.5f, 1.5f);
        [Tooltip("Much shorter than the wander interval: a follower aims at where the player was " +
                 "when it last picked, so a stale target means it trails badly across the stage.")]
        [SerializeField] private Vector2 followRepathInterval = new(0.3f, 0.9f);

        [Header("Wings")]
        [SerializeField] private float wingSpeedAtRest = 0.8f;
        [SerializeField] private float wingSpeedAtFullSpeed = 2f;

        private Rigidbody2D BeeBody => beeBody ??= GetComponent<Rigidbody2D>();

        private Random random;
        private Vector2 anchor;
        private Vector2 target;
        private Vector2 moveVelocity;
        private Vector2 moveAcceleration;

        // Bumps live in their own channel that decays on its own. Folding them into moveVelocity
        // would let the next SmoothDamp erase them within a frame or two — the same mistake that
        // made this swarm slide through itself instead of bouncing.
        private Vector2 impulseVelocity;

        private Vector3 defaultBeeScale;
        private float repathTimer;

        /// <summary>
        /// True only while there is somebody to follow. The player bee does not exist during the
        /// intro and is nulled out on destroy, so this flips back and forth over a chapter's life.
        /// </summary>
        private bool IsFollowing => followTarget != null && followTarget.Value != null;

        private float CurrentSpeed => IsFollowing ? followSpeed : moveSpeed;
        private Vector2 CurrentRoamRadius => IsFollowing ? followRoamRadius : roamRadius;
        private Vector2 CurrentRepathInterval => IsFollowing ? followRepathInterval : repathInterval;

        /// <summary>
        /// Places the bee and gives it its own deterministic wander. The generator passes a seed
        /// derived from the level, so a level's swarm looks the same every time it is played.
        /// </summary>
        /// <param name="follow">
        /// Somebody to tag along with, or null to stay put and wander. The bee harvests nothing
        /// either way — following is company, not help.
        /// </param>
        public void Initialize(Vector2 roamAnchor, int seed, Variable<Transform> follow = null)
        {
            random = new Random(seed);
            anchor = roamAnchor;
            if (follow != null) followTarget = follow;

            defaultBeeScale = baseTransform != null ? baseTransform.localScale : Vector3.one;

            BeeBody.gravityScale = 0f;
            BeeBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            BeeBody.rotation = 0f;

            // Deliberately far lighter than the player's default mass of 1. One setting gives both
            // halves of what the swarm should feel like: a shared collision throws this bee across
            // the screen and barely shifts the player — mild enough not to drag them off a flower
            // mid-dwell.
            BeeBody.mass = Mathf.Max(0.01f, mass);

            // Same reason as the player bee: this moves on FixedUpdate while the camera samples in
            // LateUpdate, so without interpolation the whole swarm shimmers against the stage.
            BeeBody.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Deliberately NOT NeverSleep. The player bee needs that because a sleeping body stops
            // receiving OnTriggerStay2D and would freeze its own dwell timer; nothing here listens
            // for triggers, so letting an idle bee sleep is free.

            BeeBody.position = anchor + RandomOffset();
            PickTarget();
        }

        private void FixedUpdate()
        {
            // Never initialized — a decor bee dropped into a scene by hand rather than generated.
            if (random == null) return;

            // Following is just the wander loop with a moving anchor. When the target goes away —
            // during the intro, or after the player bee is destroyed — the anchor keeps its last
            // value, so the bee drifts around where it last saw them rather than snapping to the
            // origin.
            if (IsFollowing) anchor = (Vector2)followTarget.Value.position + followOffset;

            repathTimer -= Time.fixedDeltaTime;

            var position = BeeBody.position;
            if (repathTimer <= 0f || Vector2.Distance(position, target) <= arriveDistance) PickTarget();

            var toTarget = target - position;
            var desired = toTarget.sqrMagnitude > 0.0001f
                ? toTarget.normalized * CurrentSpeed
                : Vector2.zero;

            moveVelocity = Vector2.SmoothDamp(
                moveVelocity, desired, ref moveAcceleration, moveSmoothTime);
            impulseVelocity = Vector2.MoveTowards(
                impulseVelocity, Vector2.zero, impulseDecay * Time.fixedDeltaTime);

            BeeBody.linearVelocity = moveVelocity + impulseVelocity;

            UpdateFacing(BeeBody.linearVelocity.x);
            UpdateWingSpeed();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            // Scaled by how fast the two were closing, so a graze and a real thump do not look the
            // same across a swarm of a dozen bees.
            var strength = Mathf.Min(
                bounceImpulse + other.relativeVelocity.magnitude * bounceSpeedScale, maxBounceImpulse);

            // Assigned, never accumulated. In a crowded swarm stacked impulses compound faster than
            // impulseDecay can bleed them off — that is exactly how the player bee used to launch
            // itself off the top of the stage.
            impulseVelocity = other.GetContact(0).normal * strength;
        }

        private void PickTarget()
        {
            target = anchor + RandomOffset();

            var interval = CurrentRepathInterval;
            repathTimer = Mathf.Lerp(interval.x, interval.y, NextUnit());
        }

        private Vector2 RandomOffset()
        {
            var radius = CurrentRoamRadius;
            return new Vector2(
                Mathf.Lerp(-radius.x, radius.x, NextUnit()),
                Mathf.Lerp(-radius.y, radius.y, NextUnit()));
        }

        private float NextUnit() => (float)random.NextDouble();

        /// <summary>Flips the sprite to face travel, matching <see cref="BeeMoveController"/>.</summary>
        private void UpdateFacing(float horizontal)
        {
            if (baseTransform == null || Mathf.Abs(horizontal) < 0.05f) return;

            var scale = defaultBeeScale;
            scale.x = Mathf.Abs(scale.x) * (horizontal < 0 ? 1 : -1);
            baseTransform.localScale = scale;
        }

        private void UpdateWingSpeed()
        {
            if (wingAnimator == null) return;

            // Total velocity, not just the steering part, so a bee that has just been walloped
            // beats its wings frantically instead of coasting along looking serene.
            var effort = CurrentSpeed > 0f
                ? Mathf.Clamp01(BeeBody.linearVelocity.magnitude / CurrentSpeed)
                : 0f;
            wingAnimator.speed = Mathf.Lerp(wingSpeedAtRest, wingSpeedAtFullSpeed, effort);
        }
    }
}
