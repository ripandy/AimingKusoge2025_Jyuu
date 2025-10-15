using UnityEngine;

namespace Kusoge.Gameplay
{
    public abstract class BeeTriggerAction : MonoBehaviour
    {
        [SerializeField] private float actionDelay = 1f;
        
        private float elapsedTime;
        
        protected bool actionRequested;
        
        protected abstract string TargetTag { get; }
        
        protected abstract void Initialize(Transform other);
        protected abstract void ExecuteAction(Transform other);
        protected abstract void Cleanup(Transform other);
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(TargetTag) && !actionRequested) return;
            elapsedTime = 0;
            Initialize(other.transform);
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(TargetTag)) return;
            
            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime < actionDelay) return;
            
            ExecuteAction(other.transform);
            elapsedTime = 0f;
            actionRequested = false;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("Flower")) return;
            Cleanup(other.transform);
            elapsedTime = 0f;
        }
    }
}