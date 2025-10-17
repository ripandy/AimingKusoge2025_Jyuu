using System;

namespace Domain
{
    [Serializable]
    public struct Game
    {
        // game data
        public int level;
        
        // Bee settings
        public int maxBees;
        public int beeDeployDelay;

        public int CollectedNectar { get; internal set; }
        public string DisplayLevel => level.ToString("D2");
        
        internal void Initialize()
        {
            CollectedNectar = 0;
        }

        internal void CollectNectar(int amount)
        {
            CollectedNectar += amount;
        }

        internal bool IsGameOver()
        {
            return false;
        }
    }

    public interface IGamePresenter
    {
        void Show(Game game);
    }
}