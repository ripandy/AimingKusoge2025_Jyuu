using System;
using Domain;
using YukiQuest.SOAR;
using TMPro;
using UnityEngine;

namespace YukiQuest.Gameplay
{
    public class BeeHivePresenter : MonoBehaviour
    {
        [SerializeField] private GameJsonableVariable game;
        [SerializeField] private TMP_Text nectarText;

        private IDisposable subscription;

        private void Start()
        {
            subscription = game.Subscribe(UpdateNectar);
        }

        private void UpdateNectar(Game g)
        {
            nectarText.text = $"{g.CollectedNectar}/{g.TargetNectar}";
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
        }
    }
}