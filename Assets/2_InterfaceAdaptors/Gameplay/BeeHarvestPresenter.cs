using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using R3;
using UnityEngine;
using UnityEngine.Assertions;

namespace Kusoge.Gameplay
{
    public class BeeHarvestPresenter : BeeTriggerAction, IBeeHarvestPresenter
    {
        protected override string TargetTag =>　"Flower";
        private readonly ReactiveProperty<int> flowerHarvested = new();
        private int currentHarvestingIndex = -1;
        
        public UniTask<int> WaitForHarvest(CancellationToken cancellationToken = default)
        {
            actionRequested = true;
            return flowerHarvested.LastAsync(cancellationToken).AsUniTask();
        }
        
        protected override void Initialize(Transform other)
        {
            currentHarvestingIndex = other.parent.GetSiblingIndex();
        }

        protected override void ExecuteAction(Transform other)
        {
            var index = other.parent.GetSiblingIndex();
            Assert.AreEqual(index, currentHarvestingIndex, "Harvesting index doesn't match!");
            
            Debug.Log($"Bee successfully harvested {other.tag} at index: {currentHarvestingIndex}");
            flowerHarvested.Value = currentHarvestingIndex;
        }

        protected override void Cleanup(Transform other)
        {
            var index = other.parent.GetSiblingIndex();
            Assert.AreEqual(index, currentHarvestingIndex, "Harvesting index doesn't match!");
            
            currentHarvestingIndex = -1;
        }
    }
}