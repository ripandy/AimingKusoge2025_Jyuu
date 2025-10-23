using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using Kusoge.SOAR;
using R3;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kusoge.Gameplay
{
    public class BeePresenter : MonoBehaviour, IBeePresenter
    {
        [SerializeField] private BeeList beeList;
        [SerializeField] private TMP_Text nectarText;
        [SerializeField] private GameObject blinkObject;
        
        private IDisposable subscription;

        private void Start()
        {
            subscription = Observable.Interval(TimeSpan.FromSeconds(3), destroyCancellationToken)
                .SubscribeAwait(async (_, token) => await TryUpdateAutoBlink(token));
        }

        public void Show(int beeId)
        {
            var bee = beeList[beeId];
            nectarText.text = $"{bee.Nectar}/{bee.Capacity}";
        }
        
        private void OnDestroy()
        {
            subscription?.Dispose();
        }
        
        private async UniTask TryUpdateAutoBlink(CancellationToken token = default)
        {
            const float blinkChance = 0.4f;
            if (Random.value > blinkChance) return;
            
            blinkObject.SetActive(true);
            
            const float blinkDuration = 0.2f;
            await UniTask.Delay(TimeSpan.FromSeconds(blinkDuration), cancellationToken: token);
            
            blinkObject.SetActive(false);
        }
    }
}