using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using R3;
using UnityEngine;

namespace YukiQuest.Gameplay
{
    public class BeeStoreNectarPresenter : BeeTriggerAction, IBeeStoreNectarPresenter
    {
        private readonly Subject<Unit> storedToHive = new();
        
        public UniTask WaitForStoreNectar(CancellationToken cancellationToken = default)
        {
            actionRequested = true;
            Debug.Log($"[{GetType().Name}][{name}] WaitForStoreNectar {actionRequested}");
            return storedToHive.FirstAsync(cancellationToken).AsUniTask();
        }
        
        protected override void Initialize(Transform other) { }

        // There is only one hive, so there is never an ambiguous target to decline.
        protected override bool TryExecuteAction(Transform other)
        {
            storedToHive.OnNext(Unit.Default);
            return true;
        }

        protected override bool Cleanup(Transform other) => true;
    }
}