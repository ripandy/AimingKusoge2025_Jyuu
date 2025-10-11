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
        public int Pollen { get; internal set; }
        public int Capacity { get; internal set; }
        public int HarvestPower { get; internal set; }
        public float PollenRate => (float)Pollen / Capacity;
        
        private float baseMoveSpeed;
        public float MoveSpeed => baseMoveSpeed * (1f - PollenRate);
        
        public bool IsFull => Pollen >= Capacity;

        internal Bee(int id)
        {
            Id = id;
            Pollen = 0;
            Capacity = 100;
            HarvestPower = 20;
            baseMoveSpeed = 5f;
        }
        
        internal void Initialize()
        {
            Pollen = 0;
            Capacity = 1;
            HarvestPower = 1;
            baseMoveSpeed = 5f;
        }
        
        internal void Carry(int amount)
        {
            var canCarry = Capacity - Pollen;
            if (canCarry <= 0) return;
            
            var carried = amount < canCarry ? amount : canCarry;
            Pollen = Math.Min(Capacity, Pollen + carried);
        }
    }

    public interface IBeePresenter
    {
        UniTask<int> WaitForHarvest(int id, CancellationToken cancellationToken = default);
        UniTask WaitForBeeHive(int id, CancellationToken cancellationToken = default);
    }
}