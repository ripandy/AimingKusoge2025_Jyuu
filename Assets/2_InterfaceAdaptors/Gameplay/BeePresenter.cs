using System;
using System.Collections.Generic;
using System.Threading;
using Kusoge.SOAR;
using Cysharp.Threading.Tasks;
using Domain;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kusoge.Gameplay
{
    public class BeePresenter : MonoBehaviour, IBeePresenter
    {
        [SerializeField] private BeeList beeList;
        [SerializeField] private FlowerList flowerList;
        [SerializeField] private GameObject beePrefab;
        [SerializeField] private Transform spawnPoint;
        
        private readonly IDictionary<int, GameObject> beeObjects = new Dictionary<int, GameObject>();
        
        private IDisposable subscription;

        private void Start()
        {
            subscription = beeList.SubscribeOnAdd(SpawnBee);
        }
        
        private void SpawnBee(Bee bee)
        {
            var beeObject = Instantiate(beePrefab, spawnPoint.position, Quaternion.identity, transform);
            
            if (beeObjects.TryGetValue(bee.Id, out var bo))
            {
                Debug.LogWarning($"Bee with ID {bee.Id} already exists. Overwriting.");
                Destroy(bo);
            }
            
            beeObjects[bee.Id] = beeObject;
            var moveController = beeObject.GetComponent<BeeMoveController>();
            moveController.Initialize(bee);
        }

        public async UniTask<int> WaitForHarvest(int id, CancellationToken cancellationToken = default)
        {
            Debug.Log($"Simulating harvest for Bee ID {id}...");
            await UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: cancellationToken);
            return Random.Range(0, flowerList.Count);
        }

        public async UniTask WaitForBeeHive(int id, CancellationToken cancellationToken = default)
        {
            Debug.Log($"Simulating arriving at BeeHive for Bee ID {id}...");
            await UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: cancellationToken);
        }
        
        private void OnDestroy()
        {
            subscription?.Dispose();
        }
    }
}