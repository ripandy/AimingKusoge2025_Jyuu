using System;

namespace Domain
{
    [Serializable]
    public struct Flower
    {
        public int pollen;
     
        public int Id { get; private set; }   
        public int CurrentPollen { get; internal set; }
        public bool IsEmpty => CurrentPollen <= 0;
        
        internal void Initialize(int id)
        {
            Id = id;
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