using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Chapters.BeeHarvest;
using Domain.Interfaces;
using UnityEngine;

namespace Domain.GameStates
{
    public class PlayGameState : IGameState, IDisposable
    {
        private Game game;
        
        private readonly IList<Bee> beeList;
        private readonly IList<Flower> flowerList;
        private readonly IList<LevelData> levels;
        private readonly IGamePresenter gamePresenter;

        private readonly IDictionary<int, IBeePresenter> beePresenters;
        private readonly IDictionary<int, IBeeHarvestPresenter> beeHarvestPresenters;
        private readonly IDictionary<int, IBeeStoreNectarPresenter> beeStoreNectarPresenters;
        private readonly IDictionary<int, IBeeAudioPresenter> beeAudioPresenters;

        private readonly IBeePresenterFactory beePresenterFactory;
        private readonly IList<IFlowerPresenter> flowerPresenters;

        public GameStateEnum Id => GameStateEnum.GamePlay;

        private CancellationTokenSource cts;
        private CancellationToken GameOverToken => cts.Token;

        private UniTaskCompletionSource<bool> gameCompletionSource;

        // Chapter 1 is a single-bee game. The remaining BeeList entries are kept as tuning data
        // for the AI helper bees that come later.
        private const int PlayerBeeId = 0;

        public PlayGameState(
            Game game,
            IList<Bee> beeList,
            IList<Flower> flowerList,
            IList<LevelData> levels,
            IGamePresenter gamePresenter,
            IDictionary<int, IBeePresenter> beePresenters,
            IDictionary<int, IBeeHarvestPresenter> beeHarvestPresenters,
            IDictionary<int, IBeeStoreNectarPresenter> beeStoreNectarPresenters,
            IDictionary<int, IBeeAudioPresenter> beeAudioPresenters,
            IBeePresenterFactory beePresenterFactory,
            IList<IFlowerPresenter> flowerPresenters)
        {
            this.game = game;
            this.beeList = beeList;
            this.flowerList = flowerList;
            this.levels = levels;
            this.gamePresenter = gamePresenter;
            this.beePresenters = beePresenters;
            this.beeHarvestPresenters = beeHarvestPresenters;
            this.beeStoreNectarPresenters = beeStoreNectarPresenters;
            this.beeAudioPresenters = beeAudioPresenters;
            this.beePresenterFactory = beePresenterFactory;
            this.flowerPresenters = flowerPresenters;
        }
        
        public async UniTask<GameStateEnum> Running(CancellationToken cancellationToken = default)
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            gameCompletionSource = new UniTaskCompletionSource<bool>();

            // NOTE: due to Game being a struct, the initialization from intro state is not reflected
            // here. Hence, re-initialize — including re-reading the quota off the level data.
            game.Initialize(levels.LevelFor(game.Level).RequiredNectar);
            gamePresenter.Show(game);

            DeployBee().Forget();

            await gameCompletionSource.Task;
            await UniTask.Yield();

            return GameStateEnum.GameOver;
        }

        private async UniTaskVoid DeployBee()
        {
            if (beeList.Count <= PlayerBeeId) return;

            var bee = beeList[PlayerBeeId];
            bee.Id = PlayerBeeId;
            beeList[PlayerBeeId] = bee;
            Debug.Log($"[{GetType().Name}] Bee deployed. id={bee.Id}");

            var (beePresenter, beeMoveController, beeHarvestPresenter, beeStoreNectarPresenter, beeAudioPresenter) =
                await beePresenterFactory.Create(bee.Id, GameOverToken);

            beePresenter.Show(bee.Id);
            beeMoveController.Initialize(bee.Id);

            beeAudioPresenter.Play(BeeAudioEnum.Mitsuda);

            beePresenters[bee.Id] = beePresenter;
            beeHarvestPresenters[bee.Id] = beeHarvestPresenter;
            beeStoreNectarPresenters[bee.Id] = beeStoreNectarPresenter;
            beeAudioPresenters[bee.Id] = beeAudioPresenter;
            
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

                // Never bail out of this loop on a bad id: the loop is the only thing that re-arms
                // WaitForHarvest, so returning here would leave the bee unable to harvest for the
                // rest of the run. Log it and let the tail re-arm.
                if (flowerId < 0 || flowerId >= flowerList.Count)
                {
                    Debug.LogError($"[{GetType().Name}] Harvested unknown flower {flowerId}; the stage " +
                                   $"and the flower list are out of sync.");
                }
                else if (!flowerList[flowerId].IsEmpty)
                {
                    var flower = flowerList[flowerId];
                    var bee = beeList[beeId];
                    var harvested = flower.Harvest(bee.harvestPower);
                    bee.Carry(harvested);
                    
                    beeList[bee.Id] = bee;
                    flowerList[flower.Id] = flower;
                    
                    Debug.Log($"[{GetType().Name}] Bee {bee.Id} harvested {harvested} from Flower {flower.Id}. Bee nectar={bee.Nectar}/{bee.capacity}, Flower nectar={flower.CurrentNectar}/{flower.MaxNectar}");

                    beePresenters[bee.Id].Show(bee.Id);
                    flowerPresenters[flower.Id].Show(flower.CurrentNectar, flower.MaxNectar);
                    
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
            if (beeList[beeId].Nectar > 0)
            {
                await beeStoreNectarPresenter.WaitForStoreNectar(GameOverToken);
                
                var bee = beeList[beeId];
                var storeAmount = bee.StoreNectar();
                game.CollectNectar(storeAmount);
                beeList[bee.Id] = bee;
                
                gamePresenter.Show(game);
                beePresenters[bee.Id].Show(bee.Id);
                Debug.Log($"[{GetType().Name}] Bee {bee.Id} stored nectar. Total nectar={game.CollectedNectar}/{game.TargetNectar}");
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: GameOverToken).SuppressCancellationThrow();
            }
            
            if (game.IsLevelCleared)
            {
                gameCompletionSource.TrySetResult(true);
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