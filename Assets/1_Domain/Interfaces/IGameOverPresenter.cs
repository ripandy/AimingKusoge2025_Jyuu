using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IGameOverPresenter
    {
        /// <summary>
        /// Presents with Game Over screen and returns true if user wants to restart the game.
        /// </summary>
        /// <returns>true to replay or false to exit.</returns>
        UniTask<bool> ShowAsync(int pollenCount, CancellationToken cancellationToken = default);
    }
}