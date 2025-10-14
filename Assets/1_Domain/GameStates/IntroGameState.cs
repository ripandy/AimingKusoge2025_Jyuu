using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;

namespace Domain.GameStates
{
    public class IntroGameState : IGameState
    {
        private readonly IList<Bee> beeList;
        private readonly IList<Flower> flowerList;
        private readonly IIntroPresenter introPresenter;
        
        public GameStateEnum Id => GameStateEnum.Intro;
        
        public IntroGameState(
            IList<Bee> beeList,
            IList<Flower> flowerList,
            IIntroPresenter introPresenter)
        {
            this.beeList = beeList;
            this.flowerList = flowerList;
            this.introPresenter = introPresenter;
        }
        
        public async UniTask<GameStateEnum> Running(CancellationToken cancellationToken = default)
        {
            var showIntroTask = introPresenter.ShowAsync(cancellationToken);
            
            foreach (var bee in beeList)
            {
                bee.Initialize();
            }

            for (var index = 0; index < flowerList.Count; index++)
            {
                var flower = flowerList[index];
                flower.Initialize(index);
            }

            await showIntroTask;
            return GameStateEnum.GamePlay;
        }
    }

    
}