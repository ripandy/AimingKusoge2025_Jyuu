using System;
using UnityEngine;

namespace Domain
{
    [Serializable]
    public struct Game
    {
        [SerializeField] internal int level;
        [SerializeField] internal int beeDeployDelay;
        [SerializeField] internal float nectarWeight;

        public float NectarWeight => nectarWeight;
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