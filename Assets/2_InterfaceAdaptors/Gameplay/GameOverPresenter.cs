using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class GameOverPresenter : MonoBehaviour, IGameOverPresenter
    {
        public UniTask<bool> ShowAsync(int pollenCount, CancellationToken cancellationToken = default)
        {
            Debug.Log($"[{GetType().Name}][{name}] Show...");
            return new UniTask<bool>();
        }
    }
}