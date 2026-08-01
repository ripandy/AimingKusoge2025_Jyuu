using System;
using Cysharp.Threading.Tasks;
using Domain;
using YukiQuest.SOAR;
using R3;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace YukiQuest.Gameplay
{
    public class BeePresenter : MonoBehaviour, IBeePresenter
    {
        [SerializeField] private BeeList beeList;
        [SerializeField] private TMP_Text nectarText;
        [SerializeField] private GameObject blinkObject;
        [SerializeField] private float blinkDuration = 0.2f;

        private bool isBlinking;
        private IDisposable subscription;

        private void Start()
        {
            subscription = Observable.Interval(TimeSpan.FromSeconds(3), destroyCancellationToken)
                .Subscribe(_ => TryAutoBlink());
        }

        public void Show(int beeId)
        {
            var bee = beeList[beeId];
            nectarText.text = $"{bee.Nectar}/{bee.Capacity}";
        }

        /// <summary>
        /// Shuts the eyes briefly. Used both by the idle blink and by flapping, which reads as
        /// effort rather than as an idle tic.
        /// </summary>
        public void Blink(float duration = -1f)
        {
            // A blink already in flight owns the eyes; re-triggering would clear them early.
            if (isBlinking || blinkObject == null) return;
            BlinkAsync(duration > 0f ? duration : blinkDuration).Forget();
        }

        private void TryAutoBlink()
        {
            const float blinkChance = 0.4f;
            if (Random.value > blinkChance) return;
            Blink();
        }

        private async UniTaskVoid BlinkAsync(float duration)
        {
            isBlinking = true;
            blinkObject.SetActive(true);

            var canceled = await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: destroyCancellationToken)
                .SuppressCancellationThrow();

            isBlinking = false;
            if (canceled) return;

            blinkObject.SetActive(false);
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
        }
    }
}