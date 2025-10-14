using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using R3;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BeeStorePollenPresenter : MonoBehaviour, IBeeStorePollenPresenter
    {
        [SerializeField] private float storageDelay = 1f;
        
        private readonly Subject<Unit> storedToHive = new();

        private bool storePollenRequested;
        
        private float elapsedTime;
        
        public UniTask WaitForStorePollen(CancellationToken cancellationToken = default)
        {
            storePollenRequested = true;
            return storedToHive.LastAsync(cancellationToken).AsUniTask();
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("BeeHive")) return;
            Debug.Log($"Bee storing pollen to {other.tag}");
            elapsedTime = 0f;
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("BeeHive")) return;
            
            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime < storageDelay) return;
            
            elapsedTime = 0f;
            storePollenRequested = false;
            
            Debug.Log("Bee successfully stored pollen to Hive");
            storedToHive.OnNext(Unit.Default);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("BeeHive")) return;
            
            Debug.Log($"Bee stopped storing pollen to {other.tag}");
            elapsedTime = 0f;
        }
    }
}