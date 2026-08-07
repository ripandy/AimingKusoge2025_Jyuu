using Soar.Variables;
using UnityEngine;

namespace YukiQuest.SOAR
{
    /// <summary>
    /// The playable extent of the current chapter's stage, in world space. Published by the
    /// chapter scene at load and read by the bee's boundary handling and the camera, which lives
    /// in the persistent Core scene and therefore cannot reference the chapter directly.
    /// </summary>
    [CreateAssetMenu(fileName = "StageBoundsVariable", menuName = "YukiQuest/StageBoundsVariable", order = -1)]
    public class StageBoundsVariable : Variable<Rect>
    {
    }
}
