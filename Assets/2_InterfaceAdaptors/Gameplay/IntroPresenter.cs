using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Interfaces;
using LitMotion;
using LitMotion.Extensions;
using Soar.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kusoge.Gameplay
{
    public class IntroPresenter : MonoBehaviour, IIntroPresenter
    {
        [SerializeField] private SoarList<AudioClip> introAudioList;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float introDelay = 2f;
        
        private AudioSource AudioSource => audioSource ??= GetComponentInChildren<AudioSource>();
        
        public async UniTask ShowAsync(CancellationToken cancellationToken = default)
        {
            Debug.Log($"[{GetType().Name}][{name}] Show...");
            
            canvasGroup.alpha = 1;
            
            await UniTask.Delay(TimeSpan.FromSeconds(introDelay), cancellationToken: cancellationToken);
            
            var index = Random.Range(0, introAudioList.Count);
            AudioSource.PlayOneShot(introAudioList[index]);
            
            await UniTask.Delay(TimeSpan.FromSeconds(introDelay), cancellationToken: cancellationToken);
            
            await LMotion.Create(1f, 0f, 1f)
                .BindToAlpha(canvasGroup)
                .ToUniTask(destroyCancellationToken);
        }
    }
}