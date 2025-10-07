using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.GameStates
{
    public class PlayGameState : IGameState, IDisposable
    {
        private Game game;
        private readonly IList<Bee> beeList;
        private readonly IList<Flower> flowerList;
        private readonly IBeePresenter beePresenter;

        public GameStateEnum Id => GameStateEnum.GamePlay;

        private CancellationTokenSource cts;
        private CancellationToken GameOverToken => cts.Token;

        private UniTaskCompletionSource<bool> gameCompletionSource;

        public PlayGameState(
            Game game,
            IList<Bee> beeList,
            IList<Flower> flowerList,
            IBeePresenter beePresenter)
        {
            this.game = game;
            this.beeList = beeList;
            this.flowerList = flowerList;
            this.beePresenter = beePresenter;
        }
        
        public async UniTask<GameStateEnum> Running(CancellationToken cancellationToken = default)
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            gameCompletionSource = new UniTaskCompletionSource<bool>();
            
            HandleBeeDeployment().Forget();
            // HandleMoveInput().Forget();
            // HandleFlapInput().Forget();

            await gameCompletionSource.Task;
            await UniTask.Yield();
            
            return GameStateEnum.GameOver;
        }

        private async UniTaskVoid HandleBeeDeployment()
        {
            while (cts != null && !GameOverToken.IsCancellationRequested)
            {
                if (beeList.Count < game.maxBees)
                {
                    DeployBee().Forget();
                }
                await UniTask.Delay(game.beeDeployDelay, cancellationToken: GameOverToken).SuppressCancellationThrow();
            }
        }

        private async UniTaskVoid DeployBee()
        {
            var bee = new Bee(Bee.ID++);
            beeList.Add(bee);

            while (cts != null && !GameOverToken.IsCancellationRequested)
            {
                var (cancelled, result) = await UniTask.WhenAny(
                    beePresenter.WaitForHarvest(bee.Id, GameOverToken),
                    beePresenter.WaitForBeeHive(bee.Id, GameOverToken))
                    .SuppressCancellationThrow();
                
                if (cancelled) break;
                
                if (result.hasResultLeft)
                {
                    HarvestPollen(result.result);
                    continue;
                }
                
                DepositPollen();
                if (!game.IsGameOver()) continue;
                
                gameCompletionSource.TrySetResult(true);
                cts?.Cancel();
            }

            void HarvestPollen(int flowerId)
            {
                var flower = flowerList[flowerId];
                if (bee.IsFull || flower.IsEmpty) return;
                
                var harvested = flower.Harvest(bee.HarvestPower);
                bee.Carry(harvested);
            }
            
            void DepositPollen()
            {
                if (bee.Pollen <= 0) return;
                
                game.CollectPollen(bee.Pollen);
                bee.Pollen = 0;
            }
        }

        public void Dispose()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
    }
}