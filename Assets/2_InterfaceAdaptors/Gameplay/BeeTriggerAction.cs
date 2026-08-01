using R3;
using UnityEngine;
using UnityEngine.UI;

namespace YukiQuest.Gameplay
{
    /// <summary>
    /// A "stay put for a moment" action: the bee must remain inside a target's trigger for
    /// <see cref="actionDelay"/> seconds, shown by a radial fill, before the action resolves.
    /// </summary>
    public abstract class BeeTriggerAction : MonoBehaviour
    {
        [SerializeField] private string targetTag = "Default";
        [SerializeField] private Image progress;
        [SerializeField] private float actionDelay = 1f;

        private float elapsedTime;

        protected bool actionRequested;

        /// <summary>True once the bee has started dwelling on a target this cycle.</summary>
        private bool IsDwelling => elapsedTime > 0f;

        protected abstract void Initialize(Transform other);

        /// <summary>
        /// Resolves the action. Return false to decline — for example when the trigger belongs to a
        /// target other than the one being dwelled on. The request then stays live and is retried,
        /// rather than being silently consumed.
        /// </summary>
        protected abstract bool TryExecuteAction(Transform other);

        /// <summary>
        /// Called when a target is left. Return false if it was not the target being tracked, so an
        /// overlapping neighbour cannot cancel an in-progress dwell.
        /// </summary>
        protected abstract bool Cleanup(Transform other);

        protected virtual void Start()
        {
            Observable.EveryValueChanged(this, _ => elapsedTime)
                .Where(_ => progress != null)
                .Subscribe(value => progress.fillAmount = Mathf.Clamp01(value / actionDelay))
                .AddTo(this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(targetTag)) return;

            // Drifting into a second target mid-dwell must not restart the timer on the first.
            if (actionRequested && IsDwelling) return;

            elapsedTime = 0f;
            Initialize(other.transform);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(targetTag) || !actionRequested) return;

            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime <= actionDelay) return;

            // Keep the request alive when the subclass declines, so the dwell resolves as soon as
            // the bee is unambiguously on one target. Consuming it here instead would leave the
            // domain awaiting a result that can never arrive, and the timer dead for good.
            if (!TryExecuteAction(other.transform)) return;

            elapsedTime = 0f;
            actionRequested = false;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(targetTag)) return;
            if (!Cleanup(other.transform)) return;

            elapsedTime = 0f;
        }
    }
}
