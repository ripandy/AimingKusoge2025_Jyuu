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

        [Header("Voice")]
        [Tooltip("Which family member voices this bee. Randomized on Awake unless the toggle below is off.")]
        [SerializeField] private BeeVoice voice;
        [SerializeField] private bool randomizeVoiceOnAwake = true;

        private AudioSource AudioSource => audioSource ??= GetComponentInChildren<AudioSource>();

        private float cooldownTimer;

        /// <summary>The family member this bee speaks as, fixed for its whole lifetime.</summary>
        public BeeVoice Voice => voice;

        private void Awake()
        {
            if (!randomizeVoiceOnAwake) return;
            voice = (BeeVoice)Random.Range(0, System.Enum.GetValues(typeof(BeeVoice)).Length);
        }

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

            var clip = PickClipInVoice(audioList);
            if (clip == null) return;

            AudioSource.PlayOneShot(clip);

            Cooldown().Forget();
        }

        /// <summary>
        /// Picks a take of this line spoken by the bee's own voice, so a single bee does not switch
        /// between five different family members mid-game.
        /// </summary>
        private AudioClip PickClipInVoice(SoarList<AudioClip> audioList)
        {
            // Selection is by clip name, not by index: the lists are not ordered consistently by
            // member (BeeAudio_Mitsuda has Ranca_4 sitting among the Ibun takes), so the same index
            // means a different speaker from one line to the next.
            var token = $"_{voice}_";
            var inVoice = audioList
                .Where(clip => clip != null &&
                               clip.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (inVoice.Count > 0) return inVoice[Random.Range(0, inVoice.Count)];

            // Not every line was recorded by every member — Pyon is Raina only, and Watashimo has no
            // Aya. Another voice beats silence for those.
            var anyVoice = audioList.Where(clip => clip != null).ToList();
            return anyVoice.Count > 0 ? anyVoice[Random.Range(0, anyVoice.Count)] : null;
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