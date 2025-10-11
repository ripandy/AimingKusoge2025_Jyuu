using Domain;
using Soar.Events;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BeeCollisionHandler : MonoBehaviour
    {
        [SerializeField] private GameEvent<(int beeId, int flowerId)> onFlowerHarvested;
        [SerializeField] private float harvestingDelay = 1f;

        private Bee bee;
        
        private int currentHarvestingIndex = -1;
        private float elapsedTime;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("Flower")) return;
            currentHarvestingIndex = other.transform.GetSiblingIndex();
            Debug.Log($"Bee harvesting {other.tag} at index: {currentHarvestingIndex}");
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("Flower")) return;
            
            var index = other.transform.GetSiblingIndex();
            if (index != currentHarvestingIndex) return;
            
            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime < harvestingDelay) return;
            
            Debug.Log($"Bee successfully harvested {other.tag} at index: {currentHarvestingIndex}");
            onFlowerHarvested.Raise((beeId: bee.Id, flowerId: currentHarvestingIndex));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag("Flower")) return;
            Debug.Log($"Bee exited collision with {other.tag}: {other.name}");
            currentHarvestingIndex = -1;
            elapsedTime = 0f;
        }
    }
}