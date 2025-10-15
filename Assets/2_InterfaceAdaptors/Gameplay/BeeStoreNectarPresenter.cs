using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using R3;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BeeStoreNectarPresenter : BeeTriggerAction, IBeeStoreNectarPresenter
    {
        protected override string TargetTag => "BeeHive";
        
        private readonly Subject<Unit> storedToHive = new();
        
        public UniTask WaitForStoreNectar(CancellationToken cancellationToken = default)
        {
            actionRequested = true;
            return storedToHive.LastAsync(cancellationToken).AsUniTask();
        }
        
        protected override void Initialize(Transform other) { }

        protected override void ExecuteAction(Transform other)
        {
            Debug.Log("Bee successfully stored nectar to Hive");
            storedToHive.OnNext(Unit.Default);
        }

        protected override void Cleanup(Transform other) { }
    }
}