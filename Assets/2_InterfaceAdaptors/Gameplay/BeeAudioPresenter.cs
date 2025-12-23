using System.Linq;
using Cysharp.Threading.Tasks;
using Domain;
using Domain.Interfaces;
using Soar;
using Soar.Collections;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BeeAudioPresenter : MonoBehaviour, IBeeAudioPresenter
    {
        [SerializeField] private SerializedKeyValuePair<BeeAudioEnum, SoarList<AudioClip>>[] beeAudioKeyValuePair;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private GameObject eyesClosedObject;
        [SerializeField] private float cooldown = 10f;
        
        private AudioSource AudioSource => audioSource ??= GetComponentInChildren<AudioSource>();
        
        private float cooldownTimer;
        
        public void Play(BeeAudioEnum beeAudio)
        {
            if (AudioSource == null || beeAudio == BeeAudioEnum.None) return;
            
            if (cooldownTimer > 0f) return;
            
            var audioList = beeAudioKeyValuePair.FirstOrDefault(pair => pair.Key == beeAudio).Value;
            if (audioList == null) return;
            
            var randomIndex = Random.Range(0, audioList.Count);
            AudioSource.PlayOneShot(audioList[randomIndex]);
            
            Cooldown().Forget();

            if (beeAudio != BeeAudioEnum.Itai) return;
            
            CloseEyes().Forget();
        }

        private async UniTaskVoid Cooldown()
        {
            cooldownTimer = cooldown;
            while (cooldownTimer > 0f && !destroyCancellationToken.IsCancellationRequested)
            {
                cooldownTimer -= Time.deltaTime;
                await UniTask.Yield(destroyCancellationToken);
            }
            cooldownTimer = 0f;
        }
        
        private async UniTaskVoid CloseEyes()
        {
            const float closeDuration = 3f;
            eyesClosedObject.SetActive(true);
            await UniTask.Delay(System.TimeSpan.FromSeconds(closeDuration), cancellationToken: destroyCancellationToken);
            eyesClosedObject.SetActive(false);
        }
        
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.gameObject.CompareTag("Bee")) return;
            Play(BeeAudioEnum.Itai);
        }
    }
}