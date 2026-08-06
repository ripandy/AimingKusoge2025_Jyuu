using System.Collections.Generic;
using YukiQuest.SOAR;
using Doinject;
using Domain;
using Domain.Chapters.BeeHarvest;
using Domain.GameStates;
using Domain.Interfaces;
using YukiQuest.Gameplay;
using Soar;
using UnityEngine;

namespace YukiQuest.Installer
{
    public class GameplayInstaller : MonoBehaviour, IBindingInstaller
    {
        [SerializeField] private BeeList beeList;
        [SerializeField] private LevelCollection levelCollection;
        [SerializeField] private GameJsonableVariable gameJsonableVariable;
        [SerializeField] private BeePresenterFactory beePresenterFactory;
        [SerializeField] private StageGenerator stageGenerator;

        [SerializeField] private IntroPresenter introPresenter;
        [SerializeField] private GameOverPresenter gameOverPresenter;

        public void Install(DIContainer container, IContextArg contextArg)
        {
            // gameJsonableVariable.LoadFromJson();
            
            // Domain
            container.BindFromInstance(gameJsonableVariable.Value);
            container.BindSingleton<IntroGameState>();
            container.BindSingleton<PlayGameState>();
            container.BindSingleton<GameOverGameState>();
            
            container.BindFromInstance<IList<Bee>>(beeList);
            container.BindFromInstance<IList<LevelData>>(levelCollection);

            // Both lists are built at level start from the stage the generator produces, so they are
            // bound empty and filled by IntroGameState — the same shape as the per-bee presenter
            // dictionaries below, which BeePresenterFactory fills on deployment.
            container.BindFromInstance<IList<Flower>>(new List<Flower>());
            container.BindFromInstance<IList<IFlowerPresenter>>(new List<IFlowerPresenter>());

            // Presenters
            container.BindFromInstance<IGamePresenter>(gameJsonableVariable);
            container.BindFromInstance<IBeePresenterFactory>(beePresenterFactory);
            container.BindFromInstance<IStagePresenter>(stageGenerator);
            container.BindFromInstance<IDictionary<int, IBeePresenter>>(new Dictionary<int, IBeePresenter>());
            container.BindFromInstance<IDictionary<int, IBeeHarvestPresenter>>(new Dictionary<int, IBeeHarvestPresenter>());
            container.BindFromInstance<IDictionary<int, IBeeStoreNectarPresenter>>(new Dictionary<int, IBeeStoreNectarPresenter>());
            container.BindFromInstance<IDictionary<int, IBeeAudioPresenter>>(new Dictionary<int, IBeeAudioPresenter>());
            container.BindFromInstance<IIntroPresenter>(introPresenter);
            container.BindFromInstance<IGameOverPresenter>(gameOverPresenter);
        }
    }
}
