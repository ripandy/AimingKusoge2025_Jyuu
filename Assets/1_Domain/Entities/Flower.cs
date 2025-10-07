using System;

namespace Domain
{
    [Serializable]
    public struct Flower
    {
        public int pollen;
        
        public int CurrentPollen { get; internal set; }
        public bool IsEmpty => CurrentPollen <= 0;
        
        internal void Initialize()
        {
            CurrentPollen = pollen;
        }
        
        internal int Harvest(int amount)
        {
            var harvested = amount < CurrentPollen ? amount : CurrentPollen;
            CurrentPollen = Math.Max(0, CurrentPollen - harvested);
            return harvested;
        }
    }
}