using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FantasyClinaryArk
{
    /// <summary>Turns pointer and submit events on the art layer into art-pixel coordinates for the kitchen.</summary>
    internal sealed class KitchenSurface : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler,
        IInitializePotentialDragHandler, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler, ISubmitHandler
    {
        public Action<Vector2> Down, Move, Up, Hover;
        public Action Leave, Submit;
        private int? pointerId;

        public bool Pressed => pointerId.HasValue;

        private Vector2 ToArt(PointerEventData e)
        {
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera ?? e.enterEventCamera, out Vector2 local);
            Rect r = rect.rect;
            return new Vector2((local.x - r.xMin) / r.width * PotatoKitchen.ArtW,
                (r.yMax - local.y) / r.height * PotatoKitchen.ArtH);
        }

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId.HasValue || e.button != PointerEventData.InputButton.Left)
                return;
            pointerId = e.pointerId;
            EventSystem.current.SetSelectedGameObject(gameObject);
            Down?.Invoke(ToArt(e));
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == pointerId)
                Move?.Invoke(ToArt(e));
        }

        public void OnPointerMove(PointerEventData e)
        {
            if (!pointerId.HasValue)
                Hover?.Invoke(ToArt(e));
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointerId)
                return;
            pointerId = null;
            Up?.Invoke(ToArt(e));
        }

        public void OnPointerEnter(PointerEventData e) => Hover?.Invoke(ToArt(e));

        public void OnPointerExit(PointerEventData e)
        {
            if (!pointerId.HasValue)
                Leave?.Invoke();
        }

        public void OnSubmit(BaseEventData e) => Submit?.Invoke();

        private void OnDisable() => pointerId = null;
    }
}
