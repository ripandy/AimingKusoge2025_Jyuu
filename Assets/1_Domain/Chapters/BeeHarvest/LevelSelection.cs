using System.Collections.Generic;
using UnityEngine;

namespace Domain.Chapters.BeeHarvest
{
    public static class LevelSelection
    {
        /// <summary>
        /// Resolves the level to play, clamping the index into range.
        /// </summary>
        /// <remarks>
        /// <see cref="Game.Level"/> arrives from a JSON save that outlives any given build, so it can
        /// point past the end of a shortened collection. Clamping keeps that a cosmetic problem
        /// rather than a crash on the first frame of the chapter.
        /// </remarks>
        public static LevelData LevelFor(this IList<LevelData> levels, int level)
        {
            if (levels == null || levels.Count == 0)
            {
                Debug.LogError($"[{nameof(LevelSelection)}] No levels authored; the stage will be empty.");
                return default;
            }

            var index = Mathf.Clamp(level, 0, levels.Count - 1);
            if (index != level)
            {
                Debug.LogWarning(
                    $"[{nameof(LevelSelection)}] Level {level} is out of range; falling back to {index}.");
            }

            return levels[index];
        }
    }
}
