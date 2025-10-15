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

        public int CollectedPollen { get; internal set; }
        public string DisplayLevel => level.ToString("D2");
        
        internal void Initialize()
        {
            CollectedPollen = 0;
        }

        internal void CollectPollen(int amount)
        {
            CollectedPollen += amount;
        }

        internal bool IsGameOver()
        {
            return false;
        }
    }
}