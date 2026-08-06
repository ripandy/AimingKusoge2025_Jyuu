namespace Domain.Chapters.BeeHarvest
{
    /// <summary>
    /// Which flower a level block holds. Names mirror the prefab filenames
    /// (<c>Flower_1.prefab</c> … <c>Flower_5.prefab</c>) so the type-to-prefab table on the stage
    /// generator is unambiguous to wire.
    ///
    /// Values are persisted inside <see cref="LevelData"/> on the level asset, so this enum is
    /// <b>append-only</b> — the same rule as <see cref="ChapterId"/>. Renaming a member is safe;
    /// reordering or reusing a number silently rewrites every authored level.
    /// </summary>
    public enum FlowerType
    {
        /// <summary>An empty block. Occupies stage width but grows nothing — this is how a level
        /// authors spacing between flowers.</summary>
        None = 0,
        Flower1 = 1,
        Flower2 = 2,
        Flower3 = 3,
        Flower4 = 4,
        Flower5 = 5,
    }
}
