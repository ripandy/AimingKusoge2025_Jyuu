using Domain.Chapters.BeeHarvest;
using Soar.Collections;
using UnityEngine;

namespace YukiQuest.SOAR
{
    /// <summary>
    /// Every level of the bee chapter, in order. <b>Level 1 is index 0.</b> This is the single
    /// authored source for a level: its nectar quota and the strip of blocks its stage is built from.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCollection", menuName = "YukiQuest/LevelCollection", order = 3)]
    public class LevelCollection : SoarList<LevelData>
    {
    }
}
