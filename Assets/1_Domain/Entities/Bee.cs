using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Domain
{
    [Serializable]
    public struct Bee
    {
        internal static int ID;
        
        [Header("Attributes")]
        [SerializeField] internal int capacity;
        [SerializeField] internal int harvestPower;
        
        [Header("Physics")]
        [SerializeField] internal float baseWeight;
        [SerializeField] private float moveForce;
        [SerializeField] private float flapForce;
        
        public int Id { get; internal set; }
        
        public int Capacity => capacity;
        public float BaseWeight => baseWeight;
        public int Nectar { get; internal set; }
        public float NectarRate => (float)Nectar / capacity;
        public bool IsFull => Nectar >= capacity;
        
        // physics
        public float MoveForce => moveForce;
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
        
        internal int StoreNectar()
        {
            if (Nectar <= 0) return 0;
            var stored = Nectar < harvestPower ? Nectar : harvestPower;
            Nectar -= stored;
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