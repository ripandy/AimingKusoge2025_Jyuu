using Domain;
using TMPro;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class FlowerPresenter : MonoBehaviour, IFlowerPresenter
    {
        [SerializeField] private GameObject nectarObject;
        [SerializeField] private GameObject emptyNectarObject;
        [SerializeField] private TMP_Text nectarText;

        private Collider2D[] colliders;

        private void Awake()
        {
            colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        }

        public void Show(int nectar, int maxNectar)
        {
            var isEmpty = nectar <= 0;
            
            foreach (var c in colliders)
            {
                c.enabled = !isEmpty;
            }
            
            nectarObject.SetActive(!isEmpty);
            emptyNectarObject.SetActive(isEmpty);
            
            nectarText.text = $"{nectar}/{maxNectar}";
        }
    }
}