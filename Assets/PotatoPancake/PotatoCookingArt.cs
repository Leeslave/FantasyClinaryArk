using UnityEngine;
using UnityEngine.UI;
using static FantasyClinaryArk.PotatoCookingBoard;

namespace FantasyClinaryArk
{
    internal readonly struct PotatoCookingArt
    {
        private static readonly Color32 Wood = new Color32(147, 85, 54, 255);
        private static readonly Color32 Grain = new Color32(167, 101, 61, 255);
        private static readonly Color32 Seam = new Color32(111, 61, 49, 255);
        private static readonly Color32 Ink = new Color32(57, 37, 39, 255);
        private static readonly Color32 Linen = new Color32(240, 201, 137, 255);
        private static readonly Color32 Gold = new Color32(255, 192, 90, 255);
        private static readonly Color32 Cream = new Color32(255, 236, 189, 255);
        private static readonly Color32 Teal = new Color32(105, 175, 157, 255);
        private static readonly Color32 Water = new Color32(140, 190, 183, 255);
        private static readonly Color32 Iron = new Color32(58, 60, 72, 255);
        private static readonly Color32 Silver = new Color32(144, 151, 158, 255);

        private readonly VertexHelper mesh;
        private readonly Rect bounds;

        internal PotatoCookingArt(VertexHelper mesh, Rect bounds)
        {
            this.mesh = mesh;
            this.bounds = bounds;
        }

        internal void Draw(CookingStep step, float time, float work, float effect,
            bool held, Vector2 pointer, Vector2 flipStart, int starchAction, bool salted, int spreadMask)
        {
            DrawCounter();
            switch (step)
            {
                case CookingStep.Grate: DrawGrater(work, time, held, pointer); break;
                case CookingStep.Drain: DrawStrainer(work, time, held); break;
                case CookingStep.RecoverStarch: DrawStarch(starchAction, effect, held, pointer); break;
                case CookingStep.Season: DrawMixing(work, salted, held, pointer); break;
                case CookingStep.Spread: DrawSpread(work, spreadMask, held, pointer); break;
                case CookingStep.Flip: DrawFlip(time, effect, flipStart); break;
            }
        }

        private void DrawCounter()
        {
            Box(0, 0, 800, 450, Wood);
            for (int row = 0; row < 5; row++)
            {
                float y = row * 94;
                Box(0, y + 3, 800, 5, Grain);
                Box(0, y, 800, 3, Seam);
                for (int mark = 0; mark < 11; mark++)
                {
                    int seed = (row * 877 + mark * 189) % 741;
                    Box(seed, y + 17 + (mark * 13) % 62, 24 + seed % 32, 2,
                        mark % 3 == 0 ? Seam : Grain);
                }
            }
            Box(28, 49, 744, 348, Seam);
            Box(28, 56, 744, 348, new Color32(196, 133, 77, 255));
            Box(38, 65, 724, 330, new Color32(205, 149, 91, 255));
            Box(44, 72, 712, 316, new Color32(197, 136, 82, 255));
            Box(49, 77, 702, 307, new Color32(210, 151, 96, 255));
            for (int mark = 0; mark < 24; mark++)
            {
                int x = 56 + (mark * 117) % 612;
                int y = 86 + (mark * 41) % 287;
                Box(x, y, 15 + (mark * 13) % 52, 2, new Color32(204, 141, 90, 255));
            }
        }

        private void DrawGrater(float work, float time, bool held, Vector2 pointer)
        {
            Vector2 rail = new Vector2(Center.x, 227);
            Oval(new Vector2(402, 111), 118, 37, Ink);
            Oval(new Vector2(402, 120), 109, 31, Teal);
            Oval(new Vector2(402, 126), 94, 21, new Color32(61, 115, 110, 255));
            Shreds(new Vector2(402, 125), 12 + (int)(work * 6), 86, 16);

            Box(342, 145, 121, 208, Ink);
            Box(348, 151, 109, 194, Iron);
            Box(355, 155, 96, 185, Silver);
            Box(355, 329, 96, 9, Cream);
            Box(364, 350, 76, 25, Iron);
            Box(377, 351, 49, 12, new Color32(210, 151, 96, 255));
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float x = 365 + col * 19 + (row % 2) * 5;
                    Box(x, 168 + row * 17, 10, 7, Cream);
                    Box(x, 171 + row * 17, 9, 5, Iron);
                }
            }

            Potato(new Vector2(140, 205), 52, 38);
            Potato(new Vector2(195, 171), 45, 33);
            Potato(new Vector2(130, 152), 43, 31);
            Vector2 moving = held ? new Vector2(Mathf.Clamp(pointer.x, 345, 455),
                Mathf.Clamp(pointer.y, 141, 316)) : rail + Vector2.up * 81;
            Potato(moving, 49 - work * 4, 36 - work * 3);
            if (held)
            {
                for (int i = 0; i < 10; i++)
                {
                    float fall = (time * 230 + i * 39) % 155;
                    Box(354 + (i * 41) % 88, Mathf.Max(126, moving.y - fall), 7, 4, Cream);
                }
            }
            Arrow(new Vector2(556, 171), new Vector2(556, 319), Teal);
            Arrow(new Vector2(601, 317), new Vector2(601, 169), Cream);
        }

        private void DrawStrainer(float work, float time, bool held)
        {
            Vector2 bowl = Center + Vector2.down * 65;
            Bowl(bowl, 143, 62, Teal, Iron);
            if (work > 0)
                Oval(bowl, 125 * work, 45 * work, Water);
            Vector2 basket = Center + Vector2.up * 45;
            if (held)
                basket.y += Mathf.Sin(time * 28) * 3;
            Stroke(basket + new Vector2(108, 8), basket + new Vector2(238, 55), Iron, 25);
            Stroke(basket + new Vector2(114, 10), basket + new Vector2(230, 52), Silver, 10);
            Oval(basket + Vector2.down * 17, 125, 72, Iron);
            Oval(basket, 133, 69, Silver);
            Oval(basket, 117, 55, Ink);
            for (int strand = -4; strand <= 4; strand++)
            {
                float y = strand * 11;
                float length = 109 - Mathf.Abs(strand) * 10;
                Stroke(basket + new Vector2(-length, y), basket + new Vector2(length, y), Silver, 1);
            }
            Oval(basket, 94, 41, Linen);
            Shreds(basket, 78, 91, 36);
            if (held)
            {
                for (int i = 0; i < 9; i++)
                {
                    float drop = (time * 152 + i * 19) % 64;
                    Oval(new Vector2(339 + (i * 37) % 118, 191 - drop), 3, 6, Water);
                }
            }
            else
                Arrow(new Vector2(400, 378), new Vector2(400, 333), Cream);
        }

        private void DrawStarch(int action, float effect, bool held, Vector2 pointer)
        {
            Bowl(MixingBowl, 100, 66, Linen, Wood);
            Shreds(MixingBowl, 42, 75, 43);
            Bowl(WasteBowl, 96, 62, Teal, Iron);
            if (action > 0)
                Oval(WasteBowl, 72, 37, Water);
            Bowl(StarchBowl, 106, 77, Silver, Iron);
            Oval(StarchBowl + Vector2.down * 23, 66, 26, Cream);
            if (action == 0 && !held)
                Oval(StarchBowl + Vector2.up * 20, 80, 37, Water);
            if (action == 1 && held)
                Oval(StarchBowl + Vector2.down * 22, 66, 26, Silver);
            if (action == 2 && effect >= 0)
                Shreds(MixingBowl, 23, 59, 40);

            if (held)
            {
                Vector2 cargo = new Vector2(Mathf.Clamp(pointer.x, 58, 742), Mathf.Clamp(pointer.y, 56, 416));
                Oval(cargo, 43, 27, action == 0 ? Water : Cream);
                Oval(cargo + new Vector2(-13, 9), 17, 6, action == 0 ? Teal : Linen);
            }
            else if (effect == 0)
            {
                Arrow(new Vector2(512, 289), new Vector2(600, 257), Cream);
                Arrow(new Vector2(288, 289), new Vector2(200, 257), Cream);
            }
        }

        private void DrawMixing(float work, bool salted, bool held, Vector2 pointer)
        {
            Bowl(Center, 173, 128, Linen, Wood);
            Oval(Center, 143, 99, Gold);
            Shreds(Center, 86, 139, 95);
            Bowl(SaltDish, 68, 49, Teal, Iron);
            if (!salted)
            {
                Oval(SaltDish, 42, 26, Cream);
                Shreds(SaltDish, 36, 40, 23);
                Arrow(SaltDish + Vector2.up * 109, SaltDish + Vector2.up * 58, Cream);
            }
            else
            {
                GuideRing(Center, 110, 84);
            }

            Vector2 spoon = held ? Center + Vector2.ClampMagnitude(pointer - Center, 131)
                : Center + new Vector2(Mathf.Cos(work * .015f) * 60, Mathf.Sin(work * .015f) * 55);
            Spoon(spoon);
        }

        private void DrawSpread(float work, int mask, bool held, Vector2 pointer)
        {
            Pan();
            float size = Mathf.Lerp(55, 151, work / SpreadSegments);
            Pancake(Center, size, size * .78f, false);
            for (int sector = 0; sector < SpreadSegments; sector++)
            {
                Vector2 target = SpreadTarget(sector);
                bool filled = (mask & (1 << sector)) != 0;
                Oval(target, 13, 13, filled ? Gold : Ink);
                Oval(target, 8, 8, filled ? Cream : Teal);
            }
            if (held)
                Spoon(Center + Vector2.ClampMagnitude(pointer - Center, 147));
        }

        private void DrawFlip(float time, float effect, Vector2 flipStart)
        {
            Pan();
            bool ready = time >= FlipReady;
            if (effect < 0)
            {
                Pancake(Center + new Vector2(-56, -22), 72, 49, false);
                Pancake(Center + new Vector2(56, 26), 59, 41, false);
            }
            else if (effect > 0)
            {
                float progress = 1 - effect / FlipAnimation;
                Vector2 air = Center + Vector2.up * Mathf.Sin(progress * Mathf.PI) * 99;
                air.x += Mathf.Clamp((flipStart.x - Center.x) * .2f, -20, 20) * Mathf.Sin(progress * Mathf.PI);
                float squash = Mathf.Lerp(1, .13f, Mathf.Sin(progress * Mathf.PI));
                Pancake(air, 145, 104 * squash, progress >= .44f);
                if (progress > .77f)
                    Sparkles(air, time);
            }
            else
            {
                Pancake(Center, 148, 107, ready);
                for (int i = 0; i < 12; i++)
                {
                    float phase = time * 4.5f + i;
                    Vector2 p = Around(Center, 160, 117, i * Mathf.PI / 6);
                    float size = 3 + (Mathf.Sin(phase) + 1) * 2;
                    Oval(p, size, size, ready ? Gold : Linen);
                }
                if (ready)
                    Arrow(new Vector2(400, 284), new Vector2(400, 366), Cream);
            }
        }

        private void Pan()
        {
            Stroke(Center + new Vector2(115, 57), new Vector2(709, 355), Ink, 43);
            Stroke(Center + new Vector2(120, 61), new Vector2(706, 357), Iron, 31);
            Stroke(new Vector2(620, 327), new Vector2(694, 352), Silver, 5);
            Oval(Center - new Vector2(0, 14), 183, 143, Ink);
            Oval(Center, 185, 140, Silver);
            Oval(Center, 172, 127, Iron);
            Oval(Center + Vector2.down * 6, 159, 114, new Color32(76, 72, 76, 255));
        }

        private void Pancake(Vector2 center, float rx, float ry, bool golden)
        {
            Oval(center + Vector2.down * 7, rx, ry, Seam);
            Oval(center, rx, ry, golden ? new Color32(185, 90, 45, 255) : Linen);
            Oval(center + Vector2.up * 5, rx * .91f, ry * .88f, golden ? Gold : Cream);
            Shreds(center, 49, rx * .86f, ry * .82f);
        }

        private void Bowl(Vector2 center, float rx, float ry, Color32 rim, Color32 inside)
        {
            Oval(center + Vector2.down * 12, rx, ry, Ink);
            Oval(center, rx, ry, rim);
            Oval(center, rx * .86f, ry * .80f, inside);
            Oval(center + Vector2.down * 10, rx * .73f, ry * .62f, rim);
        }

        private void Potato(Vector2 center, float rx, float ry)
        {
            Oval(center + new Vector2(4, -6), rx, ry, Ink);
            Oval(center, rx, ry, new Color32(162, 99, 55, 255));
            Oval(center + new Vector2(-4, 4), rx * .92f, ry * .87f, new Color32(220, 158, 87, 255));
            Oval(center + new Vector2(-10, 9), rx * .61f, ry * .57f, Gold);
            Box(center.x - 9, center.y + 4, 9, 4, Wood);
            Box(center.x + 12, center.y - 9, 6, 3, Seam);
        }

        private void Spoon(Vector2 center)
        {
            Stroke(center, center + new Vector2(68, 90), Seam, 18);
            Stroke(center, center + new Vector2(68, 90), Linen, 10);
            Oval(center, 23, 30, Seam);
            Oval(center + Vector2.up * 3, 17, 24, Grain);
        }

        private void Shreds(Vector2 center, int count, float rx, float ry)
        {
            for (int i = 0; i < count; i++)
            {
                float fraction = Mathf.Sqrt(((i * 567) % 211) / 211f);
                Vector2 p = Around(center, rx * fraction, ry * fraction, i * 2.4f);
                Box(p.x - 3, p.y, 4 + i % 7, 2 + i % 4, i % 3 == 0 ? Grain : i % 3 == 1 ? Gold : Linen);
            }
        }

        private void Sparkles(Vector2 center, float time)
        {
            for (int i = 0; i < 7; i++)
            {
                Vector2 p = Around(center, 161 + 8 * Mathf.Sin(time * 6), 128, i * .91f + time * .2f);
                Box(p.x - 2, p.y - 10, 4, 20, Cream);
                Box(p.x - 10, p.y - 2, 20, 4, Cream);
            }
        }

        private void GuideRing(Vector2 center, float rx, float ry)
        {
            for (int i = 0; i < 18; i++)
            {
                Vector2 p = Around(center, rx, ry, i * Mathf.PI / 9);
                Oval(p, 3, 3, Cream);
            }
        }

        private static Vector2 Around(Vector2 center, float rx, float ry, float angle) =>
            center + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry);

        private void Arrow(Vector2 from, Vector2 to, Color32 color)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Vector2 tail = to - direction * 21;
            Stroke(from, tail, Ink, 12);
            Stroke(from, tail, color, 6);
            Triangle(to, tail + side * 15, tail - side * 15, color);
        }

        private void Oval(Vector2 center, float rx, float ry, Color32 color)
        {
            const int sides = 24;
            int start = mesh.currentVertCount;
            Vertex(center, color);
            for (int i = 0; i < sides; i++)
                Vertex(Around(center, rx, ry, i * 2 * Mathf.PI / sides), color);
            for (int i = 0; i < sides; i++)
                mesh.AddTriangle(start, start + 1 + (i + 1) % sides, start + 1 + i);
        }

        private void Box(float x, float y, float w, float h, Color32 color) =>
            Quad(new Vector2(x, y), new Vector2(x, y + h), new Vector2(x + w, y + h), new Vector2(x + w, y), color);

        private void Stroke(Vector2 from, Vector2 to, Color32 color, float width)
        {
            Vector2 delta = (to - from).normalized;
            Vector2 side = new Vector2(-delta.y, delta.x) * (width * .5f);
            Quad(from - side, from + side, to + side, to - side, color);
        }

        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
        {
            int start = mesh.currentVertCount;
            Vertex(a, color);
            Vertex(b, color);
            Vertex(c, color);
            Vertex(d, color);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            int start = mesh.currentVertCount;
            Vertex(a, color);
            Vertex(b, color);
            Vertex(c, color);
            mesh.AddTriangle(start, start + 1, start + 2);
        }

        private void Vertex(Vector2 point, Color32 color)
        {
            Vector2 local = new Vector2(bounds.xMin + point.x * bounds.width / 800,
                bounds.yMin + point.y * bounds.height / 450);
            mesh.AddVert(local, color, Vector2.zero);
        }
    }
}
