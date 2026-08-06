using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Chapters.BeeHarvest;
using Domain.Interfaces;
using UnityEngine;

namespace Domain.GameStates
{
    public class IntroGameState : IGameState
    {
        private Game game;
        private readonly IList<Bee> beeList;
        private readonly IList<Flower> flowerList;
        private readonly IList<LevelData> levels;
        private readonly IGamePresenter gamePresenter;
        private readonly IBeePresenterFactory beePresenterFactory;
        private readonly IStagePresenter stagePresenter;
        private readonly IList<IFlowerPresenter> flowerPresenters;
        private readonly IIntroPresenter introPresenter;

        public GameStateEnum Id => GameStateEnum.Intro;

        public IntroGameState(
            Game game,
            IList<Bee> beeList,
            IList<Flower> flowerList,
            IList<LevelData> levels,
            IGamePresenter gamePresenter,
            IBeePresenterFactory beePresenterFactory,
            IStagePresenter stagePresenter,
            IList<IFlowerPresenter> flowerPresenters,
            IIntroPresenter introPresenter)
        {
            this.game = game;
            this.beeList = beeList;
            this.flowerList = flowerList;
            this.levels = levels;
            this.gamePresenter = gamePresenter;
            this.beePresenterFactory = beePresenterFactory;
            this.stagePresenter = stagePresenter;
            this.flowerPresenters = flowerPresenters;
            this.introPresenter = introPresenter;
        }

        public async UniTask<GameStateEnum> Running(CancellationToken cancellationToken = default)
        {
            var showIntroTask = introPresenter.ShowAsync(cancellationToken);

            var level = levels.LevelFor(game.Level);

            game.Initialize(level.RequiredNectar);
            gamePresenter.Show(game);
            beePresenterFactory.Clear();

            for (var index = 0; index < beeList.Count; index++)
            {
                var bee = beeList[index];
                bee.Initialize();
                beeList[index] = bee;
            }

            await BuildStage(level, cancellationToken);

            await showIntroTask;
            return GameStateEnum.GamePlay;
        }

        /// <summary>
        /// Builds the stage for <paramref name="level"/> and rebuilds the flower entity list to match
        /// it. Entities and presenters are appended together from the same walk of
        /// <see cref="LevelData.Flowers"/>, so the two lists cannot end up different lengths or
        /// disagree about which flower an id refers to.
        /// </summary>
        private async UniTask BuildStage(LevelData level, CancellationToken cancellationToken)
        {
            stagePresenter.Clear();
            var builtPresenters = await stagePresenter.BuildAsync(level, cancellationToken);

            flowerList.Clear();
            flowerPresenters.Clear();

            foreach (var (_, flowerId, block) in level.Flowers)
            {
                if (flowerId >= builtPresenters.Count)
                {
                    Debug.LogError(
                        $"[{GetType().Name}] Stage built {builtPresenters.Count} flowers but the level " +
                        $"authors more. Flower {flowerId} onwards will be unreachable.");
                    break;
                }

                var flower = new Flower();
                flower.Initialize(flowerId, block.Nectar);

                flowerList.Add(flower);
                flowerPresenters.Add(builtPresenters[flowerId]);
                flowerPresenters[flowerId].Show(flower.CurrentNectar, flower.MaxNectar);
            }

            Debug.Log($"[{GetType().Name}] Stage built. flowers={flowerList.Count}, " +
                      $"quota={level.RequiredNectar}, available={level.AvailableNectar}");
        }
    }
}
