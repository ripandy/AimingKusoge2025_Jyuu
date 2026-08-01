using R3;
using UnityEngine;
using UnityEngine.UI;

namespace YukiQuest.Gameplay
{
    /// <summary>
    /// A "stay put for a moment" action: the bee must remain inside a target's trigger for
    /// <see cref="actionDelay"/> seconds, shown by a radial fill, before the action resolves.
    ///
    /// Targets can overlap, so the bee commits to exactly one at a time and ignores the rest.
    /// Commitment is (re)taken in OnTriggerStay2D rather than OnTriggerEnter2D: drifting off one
    /// flower while already inside its neighbour produces no Enter event for that neighbour, and
    /// binding to Enter alone left the bee unable to ever commit again.
    /// </summary>
    public abstract class BeeTriggerAction : MonoBehaviour
    {
        [SerializeField] private string targetTag = "Default";
        [SerializeField] private Image progress;
        [SerializeField] private float actionDelay = 1f;

        private float elapsedTime;
        private Collider2D committedTarget;

        protected bool actionRequested;

        /// <summary>Called when the bee commits to a target.</summary>
        protected abstract void Initialize(Transform other);

        /// <summary>
        /// Resolves the action. The base class guarantees <paramref name="other"/> is the committed
        /// target, so implementations must not decline — anything awaiting this would hang.
        /// </summary>
        protected abstract void ExecuteAction(Transform other);

        /// <summary>Called when the committed target is released.</summary>
        protected abstract void Cleanup(Transform other);

        protected virtual void Start()
        {
            Observable.EveryValueChanged(this, _ => elapsedTime)
                .Where(_ => progress != null)
                .Subscribe(value => progress.fillAmount = Mathf.Clamp01(value / actionDelay))
                .AddTo(this);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(targetTag) || !actionRequested) return;

            // A flower's colliders are switched off once it runs dry, which would otherwise strand
            // the commitment on a target that can no longer report anything.
            if (committedTarget != null && !committedTarget.isActiveAndEnabled) Release();

            if (committedTarget == null)
            {
                committedTarget = other;
                elapsedTime = 0f;
                Initialize(other.transform);
            }
            else if (committedTarget != other)
            {
                return;
            }

            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime <= actionDelay) return;

            elapsedTime = 0f;
            actionRequested = false;
            committedTarget = null;

            ExecuteAction(other.transform);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(targetTag)) return;

            // Leaving a target the bee never committed to must not cancel the dwell in progress.
            if (committedTarget != null && committedTarget != other) return;

            committedTarget = null;
            elapsedTime = 0f;
            Cleanup(other.transform);
        }

        private void Release()
        {
            committedTarget = null;
            elapsedTime = 0f;
        }
    }
}
