using System;
using UnityEngine;

namespace Domain.Chapters
{
    // Inspector-authored catalog entry for a chapter. SceneName must match a scene registered in
    // Build Settings and in the AppStateManagement AppStateCollection.
    [Serializable]
    public struct ChapterInfo : IChapter
    {
        [SerializeField] private ChapterId id;
        [SerializeField] private string displayName;
        [SerializeField] private string sceneName;

        public ChapterId Id => id;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
    }
}
