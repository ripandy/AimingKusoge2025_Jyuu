using System;
using System.Collections.Generic;
using UnityEngine;

namespace Domain.Chapters.BeeHarvest
{
    /// <summary>
    /// One playable level of the bee chapter: how much nectar clears it, and the strip of blocks the
    /// stage is built from. Level order is array order on the level collection — level 1 is index 0.
    /// </summary>
    [Serializable]
    public struct LevelData
    {
        [SerializeField] private int requiredNectar;
        [SerializeField] private FlowerBlock[] blocks;

        public int RequiredNectar => requiredNectar;
        public int BlockCount => blocks?.Length ?? 0;

        /// <summary>
        /// Total nectar growing on this level. A level whose flowers hold less than
        /// <see cref="RequiredNectar"/> can never be cleared, and — because the harvest loop simply
        /// exits once every flower is drained — it soft-locks rather than failing. The level
        /// validator treats that as an error.
        /// </summary>
        public int AvailableNectar
        {
            get
            {
                if (blocks == null) return 0;

                var total = 0;
                foreach (var block in blocks)
                {
                    if (block.IsEmpty) continue;
                    total += block.Nectar;
                }

                return total;
            }
        }

        /// <summary>
        /// The single source of truth for block index → flower id. The domain walks this to build
        /// <see cref="Flower"/> entities and the stage generator walks it to instantiate prefabs, so
        /// the two numberings cannot drift apart the way the old sibling-index lookup could.
        /// </summary>
        /// <remarks>
        /// <c>FlowerId</c> is a compacted index that skips <see cref="FlowerType.None"/> blocks;
        /// <c>BlockIndex</c> is the position along the strip, which is what becomes an x coordinate.
        /// </remarks>
        public IEnumerable<(int BlockIndex, int FlowerId, FlowerBlock Block)> Flowers
        {
            get
            {
                if (blocks == null) yield break;

                var flowerId = 0;
                for (var blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
                {
                    var block = blocks[blockIndex];
                    if (block.IsEmpty) continue;

                    yield return (blockIndex, flowerId, block);
                    flowerId++;
                }
            }
        }

        public LevelData(int requiredNectar, FlowerBlock[] blocks)
        {
            this.requiredNectar = requiredNectar;
            this.blocks = blocks;
        }
    }
}
