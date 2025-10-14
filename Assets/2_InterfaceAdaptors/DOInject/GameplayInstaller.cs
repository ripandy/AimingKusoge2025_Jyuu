using System.Collections.Generic;
using Kusoge.SOAR;
using Doinject;
using Domain;
using Domain.GameStates;
using Domain.Interfaces;
using Kusoge.Gameplay;
using UnityEngine;

namespace Kusoge.Installer
{
    public class GameplayInstaller : MonoBehaviour, IBindingInstaller
    {
        [SerializeField] private GameJsonableVariable gameJsonableVariable;
        [SerializeField] private BeePresenterFactory beePresenterFactory;

        [SerializeField] private BeeHarvestPresenter beeHarvestPresenter;
        [SerializeField] private BeeStorePollenPresenter beeStorePollenPresenter;
        [SerializeField] private IntroPresenter introPresenter;
        [SerializeField] private GameOverPresenter gameOverPresenter;

        public void Install(DIContainer container, IContextArg contextArg)
        {
            // Domain
            container.BindFromInstance(gameJsonableVariable.Value);
            container.BindSingleton<IntroGameState>();
            container.BindSingleton<PlayGameState>();
            container.BindSingleton<GameOverGameState>();
            
            container.BindFromInstance<IList<Bee>>(new List<Bee>());
            container.BindFromInstance<IList<Flower>>(new List<Flower>());
            container.BindFromInstance<IBeePresenterFactory>(beePresenterFactory);

            // Presenters
            // container.BindFromInstance<IBeePresenter>(beePresenter);
            container.BindFromInstance<IDictionary<int, IBeeHarvestPresenter>>(new Dictionary<int, IBeeHarvestPresenter>());
            container.BindFromInstance<IDictionary<int, IBeeStorePollenPresenter>>(new Dictionary<int, IBeeStorePollenPresenter>());
            container.BindFromInstance<IIntroPresenter>(introPresenter);
            container.BindFromInstance<IGameOverPresenter>(gameOverPresenter);
        }
    }
}
