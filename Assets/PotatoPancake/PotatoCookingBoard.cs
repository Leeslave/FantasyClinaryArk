using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FantasyClinaryArk
{
    internal enum CookingStep { Grate, Drain, RecoverStarch, Season, Spread, Flip }

    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class PotatoCookingBoard : MaskableGraphic, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IDragHandler, ISubmitHandler
    {
        internal const float FlipReady = 3.2f;
        internal const float FlipAnimation = 1.05f;
        private const float DrainTime = 1.7f;
        private const float MixDegrees = 510f;
        internal static readonly Vector2 Center = new Vector2(400, 212);
        internal static readonly Vector2 StarchBowl = new Vector2(400, 225);
        internal static readonly Vector2 WasteBowl = new Vector2(635, 192);
        internal static readonly Vector2 MixingBowl = new Vector2(158, 196);
        internal static readonly Vector2 SaltDish = new Vector2(640, 255);
        internal const int SpreadSegments = 8;

        private Action finished;
        private Action<float, string> changed;
        private CookingStep step;
        private int? pointerId;
        private Vector2 pointer;
        private Vector2 start;
        private Vector2 previous;
        private float elapsed;
        private float work;
        private float effect;
        private int graterEnd;
        private int starchAction;
        private int spreadMask;
        private bool salted;
        private bool active;
        private Vector2 flipStart;
        private string message;

        internal void Begin(CookingStep next, Action<float, string> report, Action finish)
        {
            step = next;
            changed = report;
            finished = finish;
            pointerId = null;
            elapsed = 0;
            work = 0;
            effect = 0;
            graterEnd = 0;
            starchAction = 0;
            spreadMask = 0;
            salted = false;
            active = true;
            message = "";
            Report(0, next == CookingStep.Flip ? "가장자리가 익는 중…" : "");
            SetVerticesDirty();
        }

        private void Update()
        {
            if (!active)
                return;

            float delta = Time.unscaledDeltaTime;
            elapsed += delta;
            if (effect != 0)
            {
                bool success = effect > 0;
                effect = Mathf.MoveTowards(effect, 0, delta);
                if (effect == 0)
                {
                    if (success)
                    {
                        active = false;
                        finished.Invoke();
                        return;
                    }

                    elapsed = 0;
                    work = 0;
                    starchAction = 0;
                    Report(0, step == CookingStep.Flip ? "새 반죽을 올렸어요. 가장자리를 살펴봐요." : "감자 물을 다시 받아 뒀어요.");
                }
            }
            else if (step == CookingStep.Drain && pointerId.HasValue)
            {
                Drain(delta / DrainTime);
            }
            else if (step == CookingStep.Flip)
            {
                float heat = Mathf.Clamp01(elapsed / FlipReady);
                Report(heat, heat >= 1 ? "지금! 감자전을 위로 쓸어 뒤집어요." : message);
            }

            SetVerticesDirty();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!active || effect != 0 || pointerId.HasValue || eventData.button != PointerEventData.InputButton.Left)
                return;

            Vector2 position = Position(eventData);
            if (step == CookingStep.Season && !salted)
            {
                if (Vector2.Distance(position, SaltDish) <= 70)
                    AddSalt();
                return;
            }

            if (!CanGrab(position))
                return;

            EventSystem.current.SetSelectedGameObject(gameObject);
            pointerId = eventData.pointerId;
            pointer = start = previous = position;
            graterEnd = 0;
            if (step == CookingStep.Grate)
                Grate(position);
            if (step == CookingStep.Spread)
                Spread(position);
        }

        private bool CanGrab(Vector2 position) => step switch
        {
            CookingStep.Grate => position.x >= 300 && position.x <= 500 && position.y >= 70 && position.y <= 390,
            CookingStep.Drain => InBox(position, Center, 166, 112),
            CookingStep.RecoverStarch => InBox(position, StarchBowl, 109, 95),
            CookingStep.Season => Vector2.Distance(position, Center) <= 191,
            _ => Vector2.Distance(position, Center) <= 165
        };

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId)
                return;

            pointer = Position(eventData);
            switch (step)
            {
                case CookingStep.Grate:
                    Grate(pointer);
                    break;
                case CookingStep.Drain:
                    if (!InBox(pointer, Center, 188, 138))
                        pointerId = null;
                    break;
                case CookingStep.Season:
                    Mix(previous, pointer);
                    break;
                case CookingStep.Spread:
                    Spread(pointer);
                    break;
                case CookingStep.Flip:
                    if (pointer.y - start.y >= 104 && Mathf.Abs(pointer.x - start.x) <= 175)
                        Flip();
                    break;
            }

            previous = pointer;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId)
                return;

            Vector2 position = Position(eventData);
            pointerId = null;
            if (step == CookingStep.RecoverStarch)
                Drop(position);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!active || effect != 0 || pointerId.HasValue)
                return;

            switch (step)
            {
                case CookingStep.Grate:
                    AddStroke();
                    break;
                case CookingStep.Drain:
                    Drain(.2f);
                    break;
                case CookingStep.RecoverStarch:
                    Drop(work == 0 ? WasteBowl : MixingBowl);
                    break;
                case CookingStep.Season:
                    if (salted)
                        MixDegreesWorked(102);
                    else
                        AddSalt();
                    break;
                case CookingStep.Spread:
                    for (int sector = 0; sector < SpreadSegments; ++sector)
                    {
                        if (CoverSector(sector))
                            break;
                    }
                    break;
                case CookingStep.Flip:
                    Flip();
                    break;
            }
        }

        private void Grate(Vector2 position)
        {
            if (Mathf.Abs(position.x - Center.x) > 112)
                return;

            int end = position.y >= 292 ? 1 : position.y <= 151 ? -1 : 0;
            if (end == 0)
                return;

            if (graterEnd != 0 && end != graterEnd)
                AddStroke();
            graterEnd = end;
        }

        private void AddStroke()
        {
            work++;
            Report(work / 6, work >= 6 ? "잘 갈렸어요!" : $"{work:0} / 6");
            if (work >= 6)
                Complete();
        }

        private void Drain(float amount)
        {
            work = Mathf.Clamp01(work + amount);
            Report(work, work >= 1 ? "물기를 뺐어요!" : "감자 물을 아래 그릇에 받는 중…");
            if (work >= 1)
                Complete();
        }

        private void Drop(Vector2 position)
        {
            bool waste = InBox(position, WasteBowl, 93, 77);
            bool mix = InBox(position, MixingBowl, 103, 83);
            if ((!waste && !mix) || effect != 0)
                return;

            if ((work == 0 && mix) || (work > 0 && waste))
            {
                effect = -1.25f;
                starchAction = 2;
                Report(work / 2, work == 0
                    ? "윗물까지 섞으면 반죽이 질어져요. 맑은 물만 먼저 따라 내요."
                    : "흰 전분을 버리면 전이 찢어져요. 반죽 그릇에 넣어 봐요.");
                return;
            }

            work++;
            starchAction = (int)work;
            Report(work / 2, work == 1 ? "이제 바닥에 남은 전분을 반죽 그릇에 넣어요." : "전분을 되돌려 넣었어요!");
            if (work == 2)
                Complete();
        }

        private void AddSalt()
        {
            salted = true;
            Report(.18f, "좋아요! 이제 숟가락을 원을 그리듯 돌려요.");
        }

        private void Mix(Vector2 from, Vector2 to)
        {
            Vector2 a = from - Center;
            Vector2 b = to - Center;
            if (a.magnitude < 53 || b.magnitude < 53 || a.magnitude > 195 || b.magnitude > 195)
                return;

            float degrees = Mathf.Abs(Vector2.SignedAngle(a, b));
            if (degrees <= 105)
                MixDegreesWorked(degrees);
        }

        private void MixDegreesWorked(float degrees)
        {
            work = Mathf.Min(MixDegrees, work + degrees);
            Report(Mathf.Lerp(.18f, 1, work / MixDegrees), work >= MixDegrees ? "반죽이 골고루 섞였어요!" : message);
            if (work >= MixDegrees)
                Complete();
        }

        private void Spread(Vector2 position)
        {
            for (int sector = 0; sector < SpreadSegments; ++sector)
            {
                if ((position - SpreadTarget(sector)).sqrMagnitude <= 42 * 42 && CoverSector(sector) && effect > 0)
                    return;
            }
        }

        internal static Vector2 SpreadTarget(int sector)
        {
            float angle = sector * Mathf.PI * 2 / SpreadSegments;
            return Center + new Vector2(Mathf.Cos(angle) * 132, Mathf.Sin(angle) * 105);
        }

        private bool CoverSector(int sector)
        {
            int bit = 1 << sector;
            if ((spreadMask & bit) != 0)
                return false;

            spreadMask |= bit;
            work++;
            Report(work / SpreadSegments, work >= SpreadSegments ? "반죽이 얇게 펴졌어요!" : $"{work:0} / {SpreadSegments}");
            if (work >= SpreadSegments)
                Complete();
            return true;
        }

        private void Flip()
        {
            flipStart = pointerId.HasValue ? pointer : Center;
            pointerId = null;
            if (elapsed >= FlipReady)
            {
                Report(1, "뒤집기 성공! 반대쪽도 노릇하게 익었어요.");
                Complete(FlipAnimation);
                return;
            }

            effect = -1.3f;
            Report(elapsed / FlipReady, "너무 일찍 뒤집어서 찢어졌어요. 가장자리가 노릇해질 때까지 기다려요.");
        }

        private void Complete(float pause = .62f)
        {
            effect = pause;
            pointerId = null;
        }

        private void Report(float ratio, string status)
        {
            message = status;
            changed.Invoke(ratio, status);
        }

        private Vector2 Position(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position,
                eventData.pressEventCamera, out Vector2 local);
            Rect bounds = rectTransform.rect;
            return new Vector2((local.x - bounds.xMin) / bounds.width * 800,
                (local.y - bounds.yMin) / bounds.height * 450);
        }

        internal static bool InBox(Vector2 point, Vector2 center, float width, float height) =>
            Mathf.Abs(point.x - center.x) <= width && Mathf.Abs(point.y - center.y) <= height;

        protected override void OnDisable()
        {
            pointerId = null;
            active = false;
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (!active)
                return;

            var drawing = new PotatoCookingArt(vertices, rectTransform.rect);
            drawing.Draw(step, elapsed, work, effect, pointerId.HasValue, pointer, flipStart,
                starchAction, salted, spreadMask);
        }
    }
}
