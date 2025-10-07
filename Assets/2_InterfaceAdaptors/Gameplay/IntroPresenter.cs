using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class IntroPresenter : MonoBehaviour, IIntroPresenter
    {
        public UniTask ShowAsync(CancellationToken cancellationToken = default)
        {
            Debug.Log($"[{GetType().Name}][{name}] Show...");
            return UniTask.CompletedTask;
        }
    }
}