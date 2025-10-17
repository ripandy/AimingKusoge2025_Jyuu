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
        private readonly Subject<int> flowerHarvested = new();
        private int currentHarvestingIndex = -1;
        
        public UniTask<int> WaitForHarvest(CancellationToken cancellationToken = default)
        {
            actionRequested = true;
            Debug.Log($"[{GetType().Name}][{name}] WaitForHarvest {actionRequested}");
            return flowerHarvested.FirstAsync(cancellationToken).AsUniTask();
        }
        
        protected override void Initialize(Transform other)
        {
            currentHarvestingIndex = other.parent.GetSiblingIndex();
        }

        protected override void ExecuteAction(Transform other)
        {
            var index = other.parent.GetSiblingIndex();
            Assert.AreEqual(index, currentHarvestingIndex, "Harvesting index doesn't match!");
            
            flowerHarvested.OnNext(currentHarvestingIndex);
        }

        protected override void Cleanup(Transform other)
        {
            var index = other.parent.GetSiblingIndex();
            Assert.AreEqual(index, currentHarvestingIndex, "Harvesting index doesn't match!");
            
            currentHarvestingIndex = -1;
        }
    }
}