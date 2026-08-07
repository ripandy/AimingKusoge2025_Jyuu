using System;
using UnityEngine;

namespace Domain.Chapters.BeeHarvest
{
    /// <summary>
    /// One cell of a level's one-dimensional layout: what grows there, how much nectar it holds and
    /// how big it is. The block carries no position — where it lands in world space is
    /// <c>blockWidth * blockIndex</c>, and <c>blockWidth</c> is a presentation-layer constant.
    /// </summary>
    [Serializable]
    public struct FlowerBlock
    {
        [SerializeField] private FlowerType type;
        [SerializeField] private int nectar;
        [SerializeField] private float scale;

        public FlowerType Type => type;
        public int Nectar => nectar;

        /// <summary>
        /// Visual size multiplier. A freshly added array element deserializes to 0, which reads as
        /// "unset" rather than "invisible" — so fall back to 1 instead of collapsing the flower.
        /// </summary>
        public float Scale => scale <= 0f ? 1f : scale;

        public bool IsEmpty => type == FlowerType.None;

        public FlowerBlock(FlowerType type, int nectar, float scale = 1f)
        {
            this.type = type;
            this.nectar = nectar;
            this.scale = scale;
        }
    }
}
