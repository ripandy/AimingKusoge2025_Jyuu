namespace Domain.Chapters
{
    // Stable identifier for each mini-game chapter. Values are persisted (catalog data, save data),
    // so keep existing numbers fixed and only append new chapters.
    public enum ChapterId
    {
        None = -1,
        BeeHarvest = 0,
        DangoRoll = 1,
    }
}
