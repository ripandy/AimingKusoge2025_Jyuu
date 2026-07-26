using Domain.Chapters;
using Soar.Collections;
using UnityEngine;

namespace YukiQuest.Core
{
    // Data-driven list of available chapters, consumed by the chapter-select UI. Adding a chapter is
    // a data edit here (plus registering its scene) — no changes to core, title, or existing chapters.
    [CreateAssetMenu(fileName = "ChapterCatalog", menuName = "YukiQuest/ChapterCatalog", order = 2)]
    public class ChapterCatalog : SoarList<ChapterInfo>
    {
    }
}
