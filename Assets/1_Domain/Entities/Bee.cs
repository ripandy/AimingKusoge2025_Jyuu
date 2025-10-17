using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain
{
    [Serializable]
    public struct Bee
    {
        internal static int ID;
        
        public int Id { get; }
        public int Nectar { get; internal set; }
        public int Capacity { get; internal set; }
        public int HarvestPower { get; internal set; }
        public float NectarRate => (float)Nectar / Capacity;
        
        private float baseMoveSpeed;
        public float MoveSpeed => baseMoveSpeed * (1.2f - NectarRate);
        
        public bool IsFull => Nectar >= Capacity;

        internal Bee(int id)
        {
            Id = id;
            Nectar = 0;
            Capacity = 10;
            HarvestPower = 2;
            baseMoveSpeed = 5f;
        }
        
        internal void Carry(int amount)
        {
            var canCarry = Capacity - Nectar;
            if (canCarry <= 0) return;
            
            var carried = amount < canCarry ? amount : canCarry;
            Nectar = Math.Min(Capacity, Nectar + carried);
        }
        
        internal void StoreNectar()
        {
            Nectar = 0;
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