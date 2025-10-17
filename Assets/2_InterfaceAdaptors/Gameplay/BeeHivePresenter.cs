using System;
using Domain;
using Kusoge.SOAR;
using TMPro;
using UnityEngine;

namespace Kusoge.Gameplay
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
            nectarText.text = g.CollectedNectar.ToString();
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
        }
    }
}