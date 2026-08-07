using System;

namespace Domain
{
    /// <summary>
    /// A flower the bee can harvest. Purely a runtime entity — nothing here is authored on an asset
    /// any more; both the id and the nectar it holds come from the level's block data when the stage
    /// is built.
    /// </summary>
    [Serializable]
    public struct Flower
    {
        public int Id { get; private set; }
        public int MaxNectar { get; private set; }
        public int CurrentNectar { get; internal set; }
        public bool IsEmpty => CurrentNectar <= 0;

        internal void Initialize(int id, int nectar)
        {
            Id = id;
            MaxNectar = nectar;
            CurrentNectar = nectar;
        }

        internal int Harvest(int amount)
        {
            var harvested = amount < CurrentNectar ? amount : CurrentNectar;
            CurrentNectar -= harvested;
            return harvested;
        }
    }

    public interface IFlowerPresenter
    {
        void Show(int nectar, int maxNectar);
    }
}
