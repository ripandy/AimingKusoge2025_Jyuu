using System;

namespace Domain
{
    [Serializable]
    public struct Game
    {
        public int maxBees;
        public int beeDeployDelay;

        public int CollectedPollen { get; internal set; }
        
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