using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class IntroPresenter : MonoBehaviour, IIntroPresenter
    {
        public async UniTask ShowAsync(CancellationToken cancellationToken = default)
        {
            Debug.Log($"[{GetType().Name}][{name}] Show...");
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);
        }
    }
}