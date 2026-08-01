using System;
using UnityEngine;

namespace Domain
{
    [Serializable]
    public struct Game
    {
        [SerializeField] internal int level;
        [SerializeField] internal int[] targetNectar;

        public int CollectedNectar { get; internal set; }

        /// <summary>
        /// How much nectar must reach the hive to clear the current level.
        /// Authored per level on the GameJsonableVariable asset.
        /// </summary>
        public int TargetNectar => targetNectar != null && level >= 0 && level < targetNectar.Length
            ? targetNectar[level]
            : 0;

        public string DisplayLevel => level.ToString("D2");

        internal void Initialize()
        {
            CollectedNectar = 0;
        }

        internal void CollectNectar(int amount)
        {
            CollectedNectar += amount;
        }

        // TargetNectar == 0 means the level has no authored quota; treat it as never cleared
        // rather than instantly cleared.
        internal bool IsLevelCleared => TargetNectar > 0 && CollectedNectar >= TargetNectar;
    }

    public interface IGamePresenter
    {
        void Show(Game game);
    }
}