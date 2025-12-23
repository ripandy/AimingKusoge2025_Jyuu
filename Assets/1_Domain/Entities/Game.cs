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
        [SerializeField] internal int[] targetNectar;

        public int CollectedNectar { get; internal set; }
        // public int TargetNectar => targetNectar[level];
        public int TargetNectar { get; internal set; }
        
        public float NectarWeight => nectarWeight;
        public string DisplayLevel => level.ToString("D2");
        
        internal void Initialize()
        {
            CollectedNectar = 0;
            TargetNectar = 0;
        }

        internal void CollectNectar(int amount)
        {
            CollectedNectar += amount;
        }

        internal bool IsLevelCleared => CollectedNectar >= TargetNectar;
    }

    public interface IGamePresenter
    {
        void Show(Game game);
    }
}