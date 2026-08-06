using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.Chapters.BeeHarvest
{
    /// <summary>
    /// Builds the physical stage for a level. The domain decides <i>what</i> a level contains;
    /// everything spatial — block spacing, ground, parallax, colliders, where the hive sits — is the
    /// presentation layer's business and lives behind this interface.
    /// </summary>
    public interface IStagePresenter
    {
        /// <summary>
        /// Builds the stage and returns one presenter per flower, indexed by the <c>FlowerId</c> of
        /// <see cref="LevelData.Flowers"/>.
        /// </summary>
        UniTask<IReadOnlyList<IFlowerPresenter>> BuildAsync(
            LevelData level, CancellationToken cancellationToken = default);

        /// <summary>Tears down a previously built stage. Safe to call before the first build.</summary>
        void Clear();
    }
}
