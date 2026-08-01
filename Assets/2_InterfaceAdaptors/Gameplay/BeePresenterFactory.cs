using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using Domain.Interfaces;
using YukiQuest.SOAR;
using Soar.Variables;
using UnityEngine;

namespace YukiQuest.Gameplay
{
    public class BeePresenterFactory : MonoBehaviour, IBeePresenterFactory
    {
        [SerializeField] private BeeList beeList;
        [SerializeField] private GameObject beePrefab;
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Published so the bee prefab, which cannot reference scene objects, can find its way home.")]
        [SerializeField] private Variable<Transform> hivePoint;

        private readonly IDictionary<int, GameObject> beeObjects = new Dictionary<int, GameObject>();

        private void Awake()
        {
            if (hivePoint != null)
                hivePoint.Value = spawnPoint;
        }

        public void Clear()
        {
            foreach (var beeObject in beeObjects.Values.Where(beeObject => beeObject != null))
            {
                Destroy(beeObject);
            }

            beeObjects.Clear();
        }

        async UniTask<(IBeePresenter, IBeeMoveController, IBeeHarvestPresenter, IBeeStoreNectarPresenter, IBeeAudioPresenter)> IBeePresenterFactory.Create(int beeId, CancellationToken cancellationToken)
        {
            if (!beeObjects.TryGetValue(beeId, out var beeObject))
            {
                var beeObjectResult = await InstantiateAsync(beePrefab, transform, spawnPoint.position, Quaternion.identity).ToUniTask(cancellationToken: cancellationToken);
                beeObject = beeObjectResult.First();
                beeObjects[beeId] = beeObject;
            }
            
            var beePresenter = beeObject.GetComponent<BeePresenter>();
            var beeMoveController = beeObject.GetComponent<BeeMoveController>();
            var beeHarvestPresenter = beeObject.GetComponent<BeeHarvestPresenter>();
            var beeStoreNectarPresenter = beeObject.GetComponent<BeeStoreNectarPresenter>();
            var beeAudioPresenter = beeObject.GetComponent<BeeAudioPresenter>();
            return (beePresenter, beeMoveController, beeHarvestPresenter, beeStoreNectarPresenter, beeAudioPresenter);
        }
    }
}