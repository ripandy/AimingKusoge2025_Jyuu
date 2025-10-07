using System.Collections.Generic;
using Contents.SOAR;
using Doinject;
using Domain;
using Domain.GameStates;
using Domain.Interfaces;
using Kusoge.Gameplay;
using UnityEngine;

public class GameplayInstaller : MonoBehaviour, IBindingInstaller
{
    [SerializeField] private GameJsonableVariable gameJsonableVariable;
    [SerializeField] private BeeList beeList;
    [SerializeField] private FlowerList flowerList;

    [SerializeField] private IntroPresenter introPresenter;
    [SerializeField] private GameOverPresenter gameOverPresenter;
    
    public void Install(DIContainer container, IContextArg contextArg)
    {
        // Domain
        container.BindFromInstance(gameJsonableVariable.Value);
        container.BindSingleton<IntroGameState>();
        container.BindSingleton<PlayGameState>();
        container.BindSingleton<GameOverGameState>();
        
        container.BindFromInstance<IList<Bee>>(beeList);
        container.BindFromInstance<IList<Flower>>(flowerList);
        
        // Presenters
        container.BindFromInstance<IIntroPresenter>(introPresenter);
        container.BindFromInstance<IGameOverPresenter>(gameOverPresenter);
    }
}
