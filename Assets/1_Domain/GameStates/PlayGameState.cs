using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;
using UnityEngine;

namespace Domain.GameStates
{
    public class PlayGameState : IGameState, IDisposable
    {
        private Game game;
        
        private readonly IList<Bee> beeList;
        private readonly IList<Flower> flowerList;
        private readonly IGamePresenter gamePresenter;

        private readonly IDictionary<int, IBeePresenter> beePresenters;
        private readonly IDictionary<int, IBeeHarvestPresenter> beeHarvestPresenters;
        private readonly IDictionary<int, IBeeStoreNectarPresenter> beeStoreNectarPresenters;
        
        private readonly IBeePresenterFactory beePresenterFactory;
        private readonly IList<IFlowerPresenter> flowerPresenters;

        public GameStateEnum Id => GameStateEnum.GamePlay;

        private CancellationTokenSource cts;
        private CancellationToken GameOverToken => cts.Token;

        private UniTaskCompletionSource<bool> gameCompletionSource;

        public PlayGameState(
            Game game,
            IList<Bee> beeList,
            IList<Flower> flowerList,
            IGamePresenter gamePresenter,
            IDictionary<int, IBeePresenter> beePresenters,
            IDictionary<int, IBeeHarvestPresenter> beeHarvestPresenters,
            IDictionary<int, IBeeStoreNectarPresenter> beeStoreNectarPresenters,
            IBeePresenterFactory beePresenterFactory,
            IList<IFlowerPresenter> flowerPresenters)
        {
            this.game = game;
            this.beeList = beeList;
            this.flowerList = flowerList;
            this.gamePresenter = gamePresenter;
            this.beePresenters = beePresenters;
            this.beeHarvestPresenters = beeHarvestPresenters;
            this.beeStoreNectarPresenters = beeStoreNectarPresenters;
            this.beePresenterFactory = beePresenterFactory;
            this.flowerPresenters = flowerPresenters;
        }
        
        public async UniTask<GameStateEnum> Running(CancellationToken cancellationToken = default)
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            gameCompletionSource = new UniTaskCompletionSource<bool>();
            
            // NOTE: due to Game being a struct, the changes from other state is not reflected.
            game.Initialize();
            
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
            Debug.Log($"[{GetType().Name}] Bee deployed. id={bee.Id}");

            var (beePresenter, beeMoveController, beeHarvestPresenter, beeStoreNectarPresenter) =
                await beePresenterFactory.Create(bee.Id, GameOverToken);
            
            beePresenter.Show(bee.Id);
            beeMoveController.Initialize(bee.Id);
            
            beePresenters[bee.Id] = beePresenter;
            beeHarvestPresenters[bee.Id] = beeHarvestPresenter;
            beeStoreNectarPresenters[bee.Id] = beeStoreNectarPresenter;
            
            HandleHarvest(bee.Id, beeHarvestPresenter).Forget();
            HandleStoreNectar(bee.Id, beeStoreNectarPresenter).Forget();
        }

        private async UniTaskVoid HandleHarvest(int beeId, IBeeHarvestPresenter beeHarvestPresenter)
        {
            var canHarvest = !beeList[beeId].IsFull && flowerList.Any(f => !f.IsEmpty);
            if (canHarvest)
            {
                Debug.Log($"[{GetType().Name}] Bee {beeId} trying to harvests Flower");
                var flowerId = await beeHarvestPresenter.WaitForHarvest(GameOverToken);
                var flower = flowerList[flowerId];
                var bee = beeList[beeId];
                if (!flower.IsEmpty)
                {
                    var harvested = flower.Harvest(bee.HarvestPower);
                    bee.Carry(harvested);
                    
                    beeList[bee.Id] = bee;
                    flowerList[flower.Id] = flower;
                    
                    Debug.Log($"[{GetType().Name}] Bee {bee.Id} harvested {harvested} from Flower {flower.Id}. Bee nectar={bee.Nectar}/{bee.Capacity}, Flower nectar={flower.CurrentNectar}/{flower.nectar}");
                    
                    beePresenters[bee.Id].Show(bee.Id);
                    flowerPresenters[flower.Id].Show(flower.CurrentNectar, flower.nectar);
                    
                    // TODO: present harvested animation
                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: GameOverToken).SuppressCancellationThrow();
                }
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: GameOverToken).SuppressCancellationThrow();
            }
            
            if (cts == null || GameOverToken.IsCancellationRequested) return;
            HandleHarvest(beeId, beeHarvestPresenter).Forget();
        }
        
        private async UniTaskVoid HandleStoreNectar(int beeId, IBeeStoreNectarPresenter beeStoreNectarPresenter)
        {
            // var bee = beeList[beeId];
            if (beeList[beeId].Nectar > 0)
            {
                await beeStoreNectarPresenter.WaitForStoreNectar(GameOverToken);
                
                var bee = beeList[beeId];
                game.CollectNectar(bee.Nectar);
                bee.StoreNectar();
                beeList[bee.Id] = bee;
                
                gamePresenter.Show(game);
                beePresenters[bee.Id].Show(bee.Id);
                Debug.Log($"[{GetType().Name}] Bee {bee.Id} stored nectar. Total nectar={game.CollectedNectar}");
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: GameOverToken).SuppressCancellationThrow();
            }
            
            if (cts == null || GameOverToken.IsCancellationRequested) return;
            HandleStoreNectar(beeId, beeStoreNectarPresenter).Forget();
        }

        public void Dispose()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
    }
}