using System;
using UnityEngine;

namespace Domain
{
    [Serializable]
    public struct Game
    {
        [SerializeField] internal int level;

        public int CollectedNectar { get; internal set; }

        /// <summary>
        /// Which level to play, as an index into the chapter's level collection. Level 1 is index 0.
        /// </summary>
        public int Level => level;

        /// <summary>
        /// How much nectar must reach the hive to clear the current level, copied from the level
        /// data at <see cref="Initialize"/>.
        /// </summary>
        /// <remarks>
        /// Deliberately an auto-property, so <c>JsonUtility</c> does not persist it: the quota is
        /// authored on the level asset and must be re-read every run. A stale save file therefore
        /// cannot resurrect an old quota.
        /// </remarks>
        public int TargetNectar { get; private set; }

        public string DisplayLevel => level.ToString("D2");

        internal void Initialize(int targetNectar)
        {
            CollectedNectar = 0;
            TargetNectar = targetNectar;
        }

        internal void CollectNectar(int amount)
        {
            CollectedNectar += amount;
        }

        // TargetNectar == 0 means the level has no authored quota (or Initialize was never reached);
        // treat it as never cleared rather than instantly cleared.
        internal bool IsLevelCleared => TargetNectar > 0 && CollectedNectar >= TargetNectar;
    }

    public interface IGamePresenter
    {
        void Show(Game game);
    }
}