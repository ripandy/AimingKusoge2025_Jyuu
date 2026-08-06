using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using YukiQuest.SOAR;

namespace YukiQuest.EditorTools
{
    /// <summary>
    /// Checks authored levels for the mistakes that are invisible in the inspector but fatal in play.
    /// </summary>
    public static class LevelCollectionValidator
    {
        /// <summary>Flowers should hold comfortably more than the quota, not exactly it.</summary>
        private const float ComfortableSurplus = 1.2f;

        private const float MinReasonableScale = 0.5f;
        private const float MaxReasonableScale = 1.5f;

        [MenuItem("YukiQuest/Validate Levels")]
        public static void ValidateAll()
        {
            var collections = AssetDatabase.FindAssets($"t:{nameof(LevelCollection)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LevelCollection>)
                .Where(collection => collection != null)
                .ToList();

            if (collections.Count == 0)
            {
                Debug.LogWarning("[LevelValidator] No LevelCollection asset found.");
                return;
            }

            var problems = 0;
            foreach (var collection in collections) problems += Validate(collection);

            if (problems == 0)
            {
                Debug.Log($"[LevelValidator] {collections.Sum(c => c.Count)} level(s) checked, all good.");
            }
        }

        /// <summary>Returns the number of problems found.</summary>
        public static int Validate(LevelCollection collection)
        {
            var problems = 0;

            for (var index = 0; index < collection.Count; index++)
            {
                var level = collection[index];
                var label = $"[LevelValidator] {collection.name} level {index + 1} (index {index})";

                var flowers = level.Flowers.ToList();

                if (flowers.Count == 0)
                {
                    Debug.LogError($"{label}: has no flowers — nothing to harvest.", collection);
                    problems++;
                }

                // The soft-lock. PlayGameState only harvests while some flower still holds nectar, so
                // once they all drain the harvest loop just stops and no state ever ends the level.
                // In a chapter designed to have no fail state, that strands the player completely.
                if (level.AvailableNectar < level.RequiredNectar)
                {
                    Debug.LogError(
                        $"{label}: flowers hold {level.AvailableNectar} nectar but the quota is " +
                        $"{level.RequiredNectar}. This level cannot be finished and will soft-lock.",
                        collection);
                    problems++;
                }
                else if (level.AvailableNectar < level.RequiredNectar * ComfortableSurplus)
                {
                    Debug.LogWarning(
                        $"{label}: only {level.AvailableNectar} nectar for a quota of " +
                        $"{level.RequiredNectar}. The player must drain nearly every flower.",
                        collection);
                    problems++;
                }

                if (level.RequiredNectar <= 0)
                {
                    Debug.LogError($"{label}: quota is {level.RequiredNectar}, so the level can never " +
                                   $"be cleared.", collection);
                    problems++;
                }

                problems += ValidateBlocks(label, collection, flowers);
            }

            return problems;
        }

        private static int ValidateBlocks(
            string label,
            LevelCollection collection,
            IEnumerable<(int BlockIndex, int FlowerId, Domain.Chapters.BeeHarvest.FlowerBlock Block)> flowers)
        {
            var problems = 0;

            foreach (var (blockIndex, _, block) in flowers)
            {
                if (block.Nectar <= 0)
                {
                    Debug.LogWarning($"{label}: block {blockIndex} grows a {block.Type} with no nectar.",
                        collection);
                    problems++;
                }

                // Scale multiplies the flower's trigger radius along with its sprite, so an oversized
                // flower reaches into its neighbours' blocks and the bee can no longer tell them
                // apart. StageGenerator repeats this check against the real prefab radii at build.
                if (block.Scale < MinReasonableScale || block.Scale > MaxReasonableScale)
                {
                    Debug.LogWarning(
                        $"{label}: block {blockIndex} is scaled {block.Scale:0.##}, outside the " +
                        $"{MinReasonableScale}–{MaxReasonableScale} range that keeps flower triggers " +
                        $"inside their own block.", collection);
                    problems++;
                }
            }

            return problems;
        }
    }
}
