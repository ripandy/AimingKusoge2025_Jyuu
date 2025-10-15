using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;

namespace Domain.GameStates
{
    public class GameOverGameState : IGameState
    {
        private readonly Game game;
        private readonly IGameOverPresenter gameOverPresenter;
        
        public GameStateEnum Id => GameStateEnum.GameOver;
        
        public GameOverGameState(Game game, IGameOverPresenter gameOverPresenter)
        {
            this.game = game;
            this.gameOverPresenter = gameOverPresenter;
        }

        public async UniTask<GameStateEnum> Running(CancellationToken cancellationToken = default)
        {
            var restart = await gameOverPresenter.ShowAsync(game.CollectedNectar, cancellationToken);
            return restart ? GameStateEnum.Intro : GameStateEnum.None;
        }
    }
}