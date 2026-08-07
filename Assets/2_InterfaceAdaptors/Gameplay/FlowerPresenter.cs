using Domain;
using TMPro;
using UnityEngine;

namespace YukiQuest.Gameplay
{
    public class FlowerPresenter : MonoBehaviour, IFlowerPresenter
    {
        [SerializeField] private GameObject nectarObject;
        [SerializeField] private GameObject emptyNectarObject;
        [SerializeField] private TMP_Text nectarText;

        private Collider2D[] colliders;

        /// <summary>
        /// Which domain <see cref="Flower"/> this instance shows, assigned by
        /// <see cref="StageGenerator"/> as the stage is built.
        /// </summary>
        /// <remarks>
        /// This replaces the old scene-sibling-index lookup, which forced three separate orderings —
        /// the flower asset, the installer's presenter array and the scene hierarchy — to agree, and
        /// broke silently whenever any of them was reordered.
        /// </remarks>
        public int Id { get; internal set; }

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