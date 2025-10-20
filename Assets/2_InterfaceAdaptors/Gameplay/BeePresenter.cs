using Domain;
using Kusoge.SOAR;
using TMPro;
using UnityEngine;

namespace Kusoge.Gameplay
{
    public class BeePresenter : MonoBehaviour, IBeePresenter
    {
        [SerializeField] private BeeList beeList;
        [SerializeField] private TMP_Text nectarText;
        
        public void Show(int beeId)
        {
            var bee = beeList[beeId];
            nectarText.text = $"{bee.Nectar}/{bee.Capacity}";
        }
    }
}