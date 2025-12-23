using System.Collections.Generic;
using System.Linq;
using Kusoge.SOAR;
using Doinject;
using Domain;
using Domain.GameStates;
using Domain.Interfaces;
using Kusoge.Gameplay;
using Soar;
using UnityEngine;

namespace Kusoge.Installer
{
    public class GameplayInstaller : MonoBehaviour, IBindingInstaller
    {
        [SerializeField] private BeeList beeList;
        [SerializeField] private FlowerList flowerList;
        [SerializeField] private GameJsonableVariable gameJsonableVariable;
        [SerializeField] private BeePresenterFactory beePresenterFactory;
        
        [SerializeField] private IntroPresenter introPresenter;
        [SerializeField] private GameOverPresenter gameOverPresenter;
        
        [SerializeField] private FlowerPresenter[] flowerPresenters;

        public void Install(DIContainer container, IContextArg contextArg)
        {
            gameJsonableVariable.LoadFromJson();
            
            // Domain
            container.BindFromInstance(gameJsonableVariable.Value);
            container.BindSingleton<IntroGameState>();
            container.BindSingleton<PlayGameState>();
            container.BindSingleton<GameOverGameState>();
            
            container.BindFromInstance<IList<Bee>>(beeList);
            container.BindFromInstance<IList<Flower>>(flowerList);
            container.BindFromInstance<IList<IFlowerPresenter>>(flowerPresenters.OfType<IFlowerPresenter>().ToList());

            // Presenters
            container.BindFromInstance<IGamePresenter>(gameJsonableVariable);
            container.BindFromInstance<IBeePresenterFactory>(beePresenterFactory);
            container.BindFromInstance<IDictionary<int, IBeePresenter>>(new Dictionary<int, IBeePresenter>());
            container.BindFromInstance<IDictionary<int, IBeeHarvestPresenter>>(new Dictionary<int, IBeeHarvestPresenter>());
            container.BindFromInstance<IDictionary<int, IBeeStoreNectarPresenter>>(new Dictionary<int, IBeeStoreNectarPresenter>());
            container.BindFromInstance<IDictionary<int, IBeeAudioPresenter>>(new Dictionary<int, IBeeAudioPresenter>());
            container.BindFromInstance<IIntroPresenter>(introPresenter);
            container.BindFromInstance<IGameOverPresenter>(gameOverPresenter);
        }
    }
}
