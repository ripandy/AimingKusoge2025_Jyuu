using System;

namespace Domain
{
    [Serializable]
    public struct Flower
    {
        public int nectar;
     
        public int Id { get; private set; }   
        public int CurrentNectar { get; internal set; }
        public bool IsEmpty => CurrentNectar <= 0;
        
        internal void Initialize(int id)
        {
            Id = id;
            CurrentNectar = nectar;
        }
        
        internal int Harvest(int amount)
        {
            var harvested = amount < CurrentNectar ? amount : CurrentNectar;
            CurrentNectar = Math.Max(0, CurrentNectar - harvested);
            return harvested;
        }
    }
    
    public interface IFlowerPresenter
    {
        void Show(bool isEmpty);
    }
}