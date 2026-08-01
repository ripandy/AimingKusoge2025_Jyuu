using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Domain
{
    [Serializable]
    public struct Bee
    {
        [Header("Attributes")]
        [SerializeField] internal int capacity;
        [SerializeField] internal int harvestPower;

        [Header("Physics")]
        [SerializeField] private float moveSpeed;
        [SerializeField] private float flapForce;

        public int Id { get; internal set; }

        public int Capacity => capacity;
        public int Nectar { get; internal set; }
        public float NectarRate => (float)Nectar / capacity;
        public bool IsFull => Nectar >= capacity;

        // physics
        public float MoveSpeed => moveSpeed;
        public float FlapForce => flapForce;

        internal void Initialize()
        {
            Nectar = 0;
        }
        
        internal void Carry(int amount)
        {
            var canCarry = capacity - Nectar;
            if (canCarry <= 0) return;
            
            var carried = amount < canCarry ? amount : canCarry;
            Nectar = Math.Min(capacity, Nectar + carried);
        }
        
        /// <summary>
        /// Unloads the whole carried amount in one go. Delivering used to drip out
        /// <see cref="harvestPower"/> per visit, which meant several separate hovers at the hive to
        /// empty a single load — it read as a bug rather than a mechanic.
        /// </summary>
        internal int StoreNectar()
        {
            var stored = Nectar;
            Nectar = 0;
            return stored;
        }
    }

    public interface IBeePresenter
    {
        void Show(int beeId);
    }
    
    public interface IBeeMoveController
    {
        void Initialize(int beeId);
    }

    public interface IBeeHarvestPresenter
    {
        UniTask<int> WaitForHarvest(CancellationToken cancellationToken = default);
    }
    
    public interface IBeeStoreNectarPresenter
    {
        UniTask WaitForStoreNectar(CancellationToken cancellationToken = default);
    }
}