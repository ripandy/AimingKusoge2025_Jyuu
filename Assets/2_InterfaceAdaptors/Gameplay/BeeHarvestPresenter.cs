using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using R3;
using UnityEngine;
using UnityEngine.Assertions;

namespace Kusoge.Gameplay
{
    public class BeeHarvestPresenter : MonoBehaviour, IBeeHarvestPresenter
    {
        [SerializeField] private float harvestingDelay = 1f;
        
        private readonly ReactiveProperty<int> flowerHarvested = new();

        private bool harvestRequested;
        
        private int currentHarvestingIndex = -1;
        private float elapsedTime;
        
        public UniTask<int> WaitForHarvest(CancellationToken cancellationToken = default)
        {
            harvestRequested = true;
            return flowerHarvested.LastAsync(cancellationToken).AsUniTask();
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("Flower") && !harvestRequested) return;
            
            elapsedTime = 0f;
            currentHarvestingIndex = other.transform.GetSiblingIndex();
            
            Debug.Log($"Bee harvesting {other.tag} at index: {currentHarvestingIndex}");
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("Flower")) return;
            
            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime < harvestingDelay) return;
            
            elapsedTime = 0f;
            
            var index = other.transform.GetSiblingIndex();
            Assert.AreEqual(index, currentHarvestingIndex, "Harvesting index doesn't match!");
            
            Debug.Log($"Bee successfully harvested {other.tag} at index: {currentHarvestingIndex}");
            flowerHarvested.Value = currentHarvestingIndex;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("Flower")) return;
            
            var index = other.transform.GetSiblingIndex();
            Assert.AreEqual(index, currentHarvestingIndex, "Harvesting index doesn't match!");
            
            Debug.Log($"Bee stopped harvesting {other.tag} at index {currentHarvestingIndex}");
            currentHarvestingIndex = -1;
            elapsedTime = 0f;
        }
    }
}