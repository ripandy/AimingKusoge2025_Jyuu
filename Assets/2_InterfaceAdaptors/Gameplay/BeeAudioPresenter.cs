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

        [Tooltip("Share the cooldown with every other bee that has this ticked, so a swarm speaks as " +
                 "one voice at a time instead of a dozen at once. For the decorative bees.")]
        [SerializeField] private bool useSharedVoiceCooldown;

        private AudioSource AudioSource => audioSource ??= GetComponentInChildren<AudioSource>();

        private bool isWincing;

        /// <summary>The <see cref="Time.time"/> this bee may speak again.</summary>
        /// <remarks>
        /// A deadline rather than a counter that something has to tick down. The counter version had
        /// an owner — whichever bee started it ran the loop — and when that bee was destroyed on a
        /// scene reload its <c>UniTask.Yield</c> threw on the cancelled token, so the reset after the
        /// loop never ran. For the shared timer below that left a static stuck mid-countdown with
        /// nobody decrementing it, and the whole swarm went permanently mute on the second play of a
        /// chapter. A deadline has no owner and nothing to leak.
        /// </remarks>
        private float cooldownUntil;

        /// <summary>
        /// The swarm's shared gate. A per-bee cooldown does nothing for a crowd — ten bees each
        /// entitled to speak every few seconds is still a wall of noise — so bees that opt in queue
        /// behind one deadline.
        /// </summary>
        private static float sharedCooldownUntil;

        // Time.time restarts at zero on entering play mode while statics survive when domain reload
        // is disabled, which would otherwise gate the swarm until the clock caught up to a stale
        // deadline.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedCooldown() => sharedCooldownUntil = 0f;

        private float CooldownUntil
        {
            get => useSharedVoiceCooldown ? sharedCooldownUntil : cooldownUntil;
            set
            {
                if (useSharedVoiceCooldown) sharedCooldownUntil = value;
                else cooldownUntil = value;
            }
        }

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

            if (AudioSource == null || Time.time < CooldownUntil) return;

            var audioList = beeAudioKeyValuePair.FirstOrDefault(pair => pair.Key == beeAudio).Value;
            if (audioList == null) return;

            var clip = PickClipInVoice(audioList);
            if (clip == null) return;

            AudioSource.PlayOneShot(clip);

            CooldownUntil = Time.time + cooldown;
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

        private async UniTaskVoid CloseEyes()
        {
            const float closeDuration = 0.6f;

            // A wince already in flight owns the eyes. Without this, bumps landing inside the window
            // would each schedule their own re-open and the face would flicker — and a bee in the
            // middle of a busy swarm would end up permanently squinting.
            if (isWincing || eyesClosedObject == null) return;

            isWincing = true;
            eyesClosedObject.SetActive(true);
            await UniTask.Delay(System.TimeSpan.FromSeconds(closeDuration), cancellationToken: destroyCancellationToken);
            eyesClosedObject.SetActive(false);
            isWincing = false;
        }

        /// <summary>
        /// Anything solid is worth an "ouch". The ground gets its bounce from
        /// <see cref="BeeMoveController"/>; here it is only about the reaction, and bumping another
        /// bee deserves the same wince as bumping the floor.
        /// </summary>
        private void OnCollisionEnter2D(Collision2D other)
        {
            Play(BeeAudioEnum.Itai);
        }
    }
}