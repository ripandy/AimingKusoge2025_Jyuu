using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;

namespace Domain.GameStates
{
    public class PlayGameState : IGameState, IDisposable
    {
        private Game game;
        
        private readonly IList<Bee> beeList;
        private readonly IList<Flower> flowerList;
        
        private readonly IDictionary<int, IBeeHarvestPresenter> beeHarvestPresenters;
        private readonly IDictionary<int, IBeeStorePollenPresenter> beeStorePollenPresenters;
        
        private readonly IBeePresenterFactory beePresenterFactory;

        public GameStateEnum Id => GameStateEnum.GamePlay;

        private CancellationTokenSource cts;
        private CancellationToken GameOverToken => cts.Token;

        private UniTaskCompletionSource<bool> gameCompletionSource;

        public PlayGameState(
            Game game,
            IList<Bee> beeList,
            IList<Flower> flowerList,
            IDictionary<int, IBeeHarvestPresenter> beeHarvestPresenters,
            IDictionary<int, IBeeStorePollenPresenter> beeStorePollenPresenters,
            IBeePresenterFactory beePresenterFactory)
        {
            this.game = game;
            this.beeList = beeList;
            this.flowerList = flowerList;
            this.beeHarvestPresenters = beeHarvestPresenters;
            this.beeStorePollenPresenters = beeStorePollenPresenters;
            this.beePresenterFactory = beePresenterFactory;
        }
        
        public async UniTask<GameStateEnum> Running(CancellationToken cancellationToken = default)
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            gameCompletionSource = new UniTaskCompletionSource<bool>();
            
            HandleBeeDeployment().Forget();

            await gameCompletionSource.Task;
            await UniTask.Yield();
            
            return GameStateEnum.GameOver;
        }

        private async UniTaskVoid HandleBeeDeployment()
        {
            if (beeList.Count < game.maxBees)
                DeployBee().Forget();
            
            await UniTask.Delay(TimeSpan.FromSeconds(game.beeDeployDelay), cancellationToken: GameOverToken).SuppressCancellationThrow();
            
            if (cts == null || GameOverToken.IsCancellationRequested) return;
            
            HandleBeeDeployment().Forget();
        }

        private async UniTaskVoid DeployBee()
        {
            var bee = new Bee(Bee.ID++);
            beeList.Add(bee);
            
            var beeMoveController = await beePresenterFactory.CreateBeeMoveController(bee, GameOverToken);
            var beeHarvestPresenter = await beePresenterFactory.CreateBeeHarvestPresenter(bee, GameOverToken);
            var beeStorePollenPresenter = await beePresenterFactory.CreateBeeStorePollenPresenter(bee, GameOverToken);
            
            beeMoveController.Initialize(bee);
            beeHarvestPresenters[bee.Id] = beeHarvestPresenter;
            beeStorePollenPresenters[bee.Id] = beeStorePollenPresenter;
            
            HandleHarvest(bee, beeHarvestPresenter).Forget();
            HandleStorePollen(bee, beeStorePollenPresenter).Forget();
        }

        private async UniTaskVoid HandleHarvest(Bee bee, IBeeHarvestPresenter beeHarvestPresenter)
        {
            var canHarvest = !bee.IsFull && flowerList.Any(f => !f.IsEmpty);
            if (canHarvest)
            {
                var flowerId = await beeHarvestPresenter.WaitForHarvest(GameOverToken);
                var flower = flowerList[flowerId];
                if (!flower.IsEmpty)
                {
                    var harvested = flower.Harvest(bee.HarvestPower);
                    bee.Carry(harvested);
                    
                    // TODO: present harvested animation
                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: GameOverToken).SuppressCancellationThrow();
                }
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: GameOverToken).SuppressCancellationThrow();
            }
            
            if (cts == null || GameOverToken.IsCancellationRequested) return;
            HandleHarvest(bee, beeHarvestPresenter).Forget();
        }
        
        private async UniTaskVoid HandleStorePollen(Bee bee, IBeeStorePollenPresenter beeStorePollenPresenter)
        {
            if (bee.Pollen > 0)
            {
                await beeStorePollenPresenter.WaitForStorePollen(GameOverToken);
                game.CollectPollen(bee.Pollen);
                bee.Pollen = 0;
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: GameOverToken).SuppressCancellationThrow();
            }
            
            if (cts == null || GameOverToken.IsCancellationRequested) return;
            HandleStorePollen(bee, beeStorePollenPresenter).Forget();
        }

        public void Dispose()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
    }
}