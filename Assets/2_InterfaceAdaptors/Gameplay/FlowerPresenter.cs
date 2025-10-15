using Domain;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class FlowerPresenter : MonoBehaviour, IFlowerPresenter
    {
        [SerializeField] private GameObject pollenObject;
        [SerializeField] private GameObject emptyPollenObject;
        
        public void Show(bool isEmpty)
        {
            pollenObject.SetActive(!isEmpty);
            emptyPollenObject.SetActive(isEmpty);
        }
    }
}