using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.GameStates
{
    public interface IGameState
    {
        GameStateEnum Id { get; }
        UniTask<GameStateEnum> Running(CancellationToken cancellationToken = default);
    }
}