namespace Domain.Chapters
{
    // Metadata describing a chapter. The chapter's actual gameplay lives in its own scene
    // (loaded by name via AppStateManagement), so this contract carries no gameplay logic.
    public interface IChapter
    {
        ChapterId Id { get; }
        string DisplayName { get; }
        string SceneName { get; }
    }
}
