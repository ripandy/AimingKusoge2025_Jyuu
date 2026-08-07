using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace YukiQuest.Gameplay
{
    public class GameOverPresenter : MonoBehaviour, IGameOverPresenter
    {
        [SerializeField] private GameObject uiGameObject;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        private void Start()
        {
            uiGameObject.SetActive(false);
        }

        public async UniTask<bool> ShowAsync(int pollenCount, CancellationToken cancellationToken = default)
        {
            Debug.Log($"[{GetType().Name}][{name}] Show...");
            uiGameObject.SetActive(true);

            // var restartButtonTask = restartButton.OnClickAsync(cancellationToken);
            var exitButtonTask = exitButton.OnClickAsync(cancellationToken);
            // await UniTask.WhenAny(restartButtonTask, exitButtonTask);
            await exitButtonTask;
            return true;
        }
    }
}