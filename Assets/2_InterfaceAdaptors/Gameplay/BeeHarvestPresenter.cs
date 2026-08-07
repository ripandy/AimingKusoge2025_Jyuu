using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using R3;
using UnityEngine;

namespace YukiQuest.Gameplay
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
            harvestingIndex.Value = FlowerIdOf(other);
        }

        protected override void ExecuteAction(Transform other)
        {
            // Only ever called for the flower the bee committed to, so no index check is needed —
            // and declining here would hang the harvest loop awaiting this subject.
            flowerHarvested.OnNext(FlowerIdOf(other));
        }

        /// <summary>
        /// Resolves the domain flower id from the collider the bee is dwelling on. The trigger sits
        /// on a child of the flower root, so the presenter is found by walking up.
        /// </summary>
        private int FlowerIdOf(Transform other)
        {
            var presenter = other.GetComponentInParent<FlowerPresenter>();
            if (presenter != null) return presenter.Id;

            Debug.LogError($"[{GetType().Name}][{name}] {other.name} is tagged as a flower but has no " +
                           $"FlowerPresenter above it.");
            return -1;
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