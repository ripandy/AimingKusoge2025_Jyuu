using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using R3;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BeeHarvestPresenter : BeeTriggerAction, IBeeHarvestPresenter
    {
        [SerializeField] private BeeAudioPresenter beeAudioPresenter;
        [SerializeField] private GameObject mouthCloseObject;
        
        private readonly Subject<int> flowerHarvested = new();
        private readonly ReactiveProperty<int> harvestingIndex = new(-1);
        
        private int CurrentHarvestingIndex => harvestingIndex.Value;
        
        private IDisposable subscription;
        
        protected override void Start()
        {
            base.Start();
            subscription = harvestingIndex.Pairwise().Subscribe(OnHarvestingIndexChanged);
            MunchingMouth(destroyCancellationToken).Forget();
        }
        
        public UniTask<int> WaitForHarvest(CancellationToken cancellationToken = default)
        {
            actionRequested = true;
            Debug.Log($"[{GetType().Name}][{name}] WaitForHarvest {actionRequested}");
            return flowerHarvested.FirstAsync(cancellationToken).AsUniTask();
        }

        protected override void Initialize(Transform other)
        {
            harvestingIndex.Value = other.parent.GetSiblingIndex();
        }

        protected override void ExecuteAction(Transform other)
        {
            var index = other.parent.GetSiblingIndex();
            
            if (CurrentHarvestingIndex == -1 || index != CurrentHarvestingIndex) return;
            flowerHarvested.OnNext(CurrentHarvestingIndex);
        }

        protected override void Cleanup(Transform other)
        {
            harvestingIndex.Value = -1;
        }
        
        private void OnHarvestingIndexChanged((int Previous, int Current) indices)
        {
            if (!actionRequested) return;
            
            var (previous, current) = indices;
            if (current < 0) return;
            if (previous >= 0 && previous == current) return;
            beeAudioPresenter.Play(BeeAudioEnum.MoguMogu);
        }
        
        private async UniTaskVoid MunchingMouth(CancellationToken token = default)
        {
            const float munchDuration = 0.25f;
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(munchDuration), cancellationToken: token);
                mouthCloseObject.SetActive(CurrentHarvestingIndex >= 0 && actionRequested);
                await UniTask.Delay(TimeSpan.FromSeconds(munchDuration), cancellationToken: token);
                mouthCloseObject.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            subscription?.Dispose();
            flowerHarvested.Dispose();
            harvestingIndex.Dispose();
        }
    }
}