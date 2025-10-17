using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;
using UnityEngine;

namespace Domain.GameStates
{
    public class IntroGameState : IGameState
    {
        private Game game;
        
        private readonly IList<Bee> beeList;
        private readonly IList<Flower> flowerList;
        
        private readonly IGamePresenter gamePresenter;
        private readonly IList<IFlowerPresenter> flowerPresenters;
        private readonly IIntroPresenter introPresenter;
        
        public GameStateEnum Id => GameStateEnum.Intro;
        
        public IntroGameState(
            IList<Bee> beeList,
            IList<Flower> flowerList,
            IGamePresenter gamePresenter,
            IList<IFlowerPresenter> flowerPresenters,
            IIntroPresenter introPresenter)
        {
            this.beeList = beeList;
            this.flowerList = flowerList;
            this.gamePresenter = gamePresenter;
            this.flowerPresenters = flowerPresenters;
            this.introPresenter = introPresenter;
        }
        
        public async UniTask<GameStateEnum> Running(CancellationToken cancellationToken = default)
        {
            var showIntroTask = introPresenter.ShowAsync(cancellationToken);
            
            game.Initialize();
            beeList.Clear();

            gamePresenter.Show(game);
            
            for (var index = 0; index < flowerList.Count; index++)
            {
                var flower = flowerList[index];
                flower.Initialize(index);
                flowerList[index] = flower;
                flowerPresenters[flower.Id].Show(flower.CurrentNectar, flower.nectar);
                Debug.Log($"[{GetType().Name}] Flower initialized. id={flower.Id}, nectar={flower.CurrentNectar}/{flower.nectar}");
            }

            await showIntroTask;
            return GameStateEnum.GamePlay;
        }
    }

    
}