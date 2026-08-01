using System.Linq;
using Cysharp.Threading.Tasks;
using Domain;
using Domain.Interfaces;
using Soar;
using Soar.Collections;
using UnityEngine;

namespace YukiQuest.Gameplay
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
            if (beeAudio == BeeAudioEnum.None) return;

            // The bump face is a reaction, not a line of dialogue — it must not be swallowed by the
            // voice cooldown, or the bee would only wince once every few bumps.
            if (beeAudio == BeeAudioEnum.Itai)
                CloseEyes().Forget();

            if (AudioSource == null || cooldownTimer > 0f) return;

            var audioList = beeAudioKeyValuePair.FirstOrDefault(pair => pair.Key == beeAudio).Value;
            if (audioList == null) return;

            var randomIndex = Random.Range(0, audioList.Count);
            AudioSource.PlayOneShot(audioList[randomIndex]);

            Cooldown().Forget();
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
            const float closeDuration = 0.6f;
            eyesClosedObject.SetActive(true);
            await UniTask.Delay(System.TimeSpan.FromSeconds(closeDuration), cancellationToken: destroyCancellationToken);
            eyesClosedObject.SetActive(false);
        }

        // Bees no longer collide with each other, so the "ouch" face now belongs to ground bumps.
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.gameObject.CompareTag("Bounds")) return;
            Play(BeeAudioEnum.Itai);
        }
    }
}