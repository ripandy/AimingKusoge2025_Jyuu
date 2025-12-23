using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using R3;
using UnityEngine;

namespace Kusoge.Gameplay
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

        protected override void ExecuteAction(Transform other)
        {
            storedToHive.OnNext(Unit.Default);
        }

        protected override void Cleanup(Transform other) { }
    }
}