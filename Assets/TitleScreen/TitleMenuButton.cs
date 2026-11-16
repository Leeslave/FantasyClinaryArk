using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FantasyClinaryArk
{
    [RequireComponent(typeof(Button))]
    public sealed class TitleMenuButton : MonoBehaviour, IPointerEnterHandler
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.IsInteractable())
                button.Select();
        }
    }
}
