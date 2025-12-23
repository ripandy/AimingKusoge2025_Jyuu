using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Kusoge.Gameplay
{
    public abstract class BeeTriggerAction : MonoBehaviour
    {
        [SerializeField] private string targetTag = "Default";
        [SerializeField] private Image progress;
        [SerializeField] private float actionDelay = 1f;
        
        private float elapsedTime;
        
        protected bool actionRequested;
        
        protected abstract void Initialize(Transform other);
        protected abstract void ExecuteAction(Transform other);
        protected abstract void Cleanup(Transform other);

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
            elapsedTime = 0;
            Initialize(other.transform);
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(targetTag) || !actionRequested) return;
            
            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime <= actionDelay) return;
            
            elapsedTime = 0f;
            actionRequested = false;
            
            ExecuteAction(other.transform);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(targetTag)) return;
            elapsedTime = 0f;
            Cleanup(other.transform);
        }
    }
}