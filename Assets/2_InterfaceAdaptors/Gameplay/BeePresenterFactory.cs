using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain;
using Domain.Interfaces;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BeePresenterFactory : MonoBehaviour, IBeePresenterFactory
    {
        [SerializeField] private GameObject beePrefab;
        [SerializeField] private Transform spawnPoint;
        
        private readonly IDictionary<int, GameObject> beeObjects = new Dictionary<int, GameObject>();

        public async UniTask<IBeeMoveController> CreateBeeMoveController(Bee bee, CancellationToken cancellationToken = default)
        {
            if (beeObjects.TryGetValue(bee.Id, out var beeObject))
                return beeObject.GetComponent<BeeMoveController>();
            
            var beeObjectResult = await InstantiateAsync(beePrefab, transform, spawnPoint.position, Quaternion.identity).ToUniTask(cancellationToken: cancellationToken);
            beeObject = beeObjectResult.First();
            beeObjects[bee.Id] = beeObject;
            
            return beeObject.GetComponent<BeeMoveController>();
        }

        public async UniTask<IBeeHarvestPresenter> CreateBeeHarvestPresenter(Bee bee, CancellationToken cancellationToken = default)
        {
            if (beeObjects.TryGetValue(bee.Id, out var beeObject))
                return beeObject.GetComponent<BeeHarvestPresenter>();
            
            var beeObjectResult = await InstantiateAsync(beePrefab, transform, spawnPoint.position, Quaternion.identity).ToUniTask(cancellationToken: cancellationToken);
            beeObject = beeObjectResult.First();
            beeObjects[bee.Id] = beeObject;
            
            return beeObject.GetComponent<BeeHarvestPresenter>();
        }

        public async UniTask<IBeeStorePollenPresenter> CreateBeeStorePollenPresenter(Bee bee, CancellationToken cancellationToken = default)
        {
            if (beeObjects.TryGetValue(bee.Id, out var beeObject))
                return beeObject.GetComponent<IBeeStorePollenPresenter>();
            
            var beeObjectResult = await InstantiateAsync(beePrefab, transform, spawnPoint.position, Quaternion.identity).ToUniTask(cancellationToken: cancellationToken);
            beeObject = beeObjectResult.First();
            beeObjects[bee.Id] = beeObject;
            
            return beeObject.GetComponent<IBeeStorePollenPresenter>();
        }
    }
}