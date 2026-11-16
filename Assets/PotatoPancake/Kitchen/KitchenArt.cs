using System;
using System.Collections.Generic;
using UnityEngine;
using static FantasyClinaryArk.Px;

namespace FantasyClinaryArk
{
    internal sealed class PotatoShape
    {
        public int W, H, Count;
        public bool[] Ins;
        public float[] L;
    }

    internal sealed class BowlSprites
    {
        public PixelSprite Back, Front;
        public int Ry0;
    }

    /// <summary>Palettes and every procedurally painted prop, ported from the web prototype.</summary>
    internal static class KitchenArt
    {
        // Warm pixel-art ramps, dark → light.
        public static readonly Color32 Out = Hex("#2a1719");
        public static readonly Color32[] Skin = Ramp("#4a2c1a", "#6e4426", "#8f5c33", "#ad7643", "#c89258", "#dcae74");
        public static readonly Color32[] Flesh = Ramp("#a07a38", "#c09a4a", "#d8b862", "#e9cf7e", "#f5e3a2", "#fff4cc");
        public static readonly Color32[] Raw = Ramp("#bb9a58", "#cfb06c", "#ddc283", "#e9d39b", "#f3e2b6", "#fbf0d4");
        public static readonly Color32[] Steel = Ramp("#3d434d", "#5d6470", "#7f8794", "#a4acb8", "#c9d0d9", "#eef2f6");
        public static readonly Color32[] Wood = Ramp("#4a2618", "#6b3923", "#8a4f30", "#a3623a", "#bb7848", "#d08f5a");
        public static readonly Color32[] Board = Ramp("#7a4c2a", "#946038", "#ad7646", "#c48d58", "#d6a46c", "#e4ba82");
        public static readonly Color32[] Iron = Ramp("#121216", "#1b1b21", "#25252d", "#30303a", "#40404c", "#5a5b69");
        public static readonly Color32[] Cloth = Ramp("#8f897c", "#b8b1a2", "#d9d3c5", "#efeadf", "#fbf9f3");
        public static readonly Color32[] Starch = Ramp("#cfc9bb", "#e2ddd1", "#f1ede4", "#fbf9f4");
        public static readonly Color32[] Ceram = Ramp("#9c8c78", "#bfb09b", "#dccfbd", "#efe5d6", "#fbf5ec");
        public static readonly Color32[] Blue = Ramp("#2c3f73", "#3f5aa0", "#6f8bd0");
        public static readonly Color32[] Brick = Ramp("#2e2230", "#3b2d3a", "#4a3a47", "#584654", "#66525f", "#76616c");
        public static readonly Color32[] Stone = Ramp("#2a2328", "#3a3138", "#4a3f47", "#5b4e56", "#6d5e66");
        public static readonly Color32[] GlassR = Ramp("#7d9aa6", "#a9c4cc", "#d4e6ea");
        public static readonly Color32[] OilR = Ramp("#8a5a12", "#b07c1c", "#d4a632", "#ecc95a");
        public static readonly Color32[] Cook = Ramp("#f1dca6", "#efcd82", "#e8b65a", "#d9963c", "#b8702a", "#86481c", "#4a2614", "#24120c");
        public static readonly Color32 Eye = Hex("#3a2214");
        public static readonly Color32 Cut = Hex("#c9a85a");

        private static readonly Color32[] Plastic = Ramp("#140f10", "#231a1b", "#382a2a", "#4d3b3a");
        private static readonly Color32[] Ribbon = Ramp("#5a1e23", "#7A2A30", "#9a3a3e");
        private static readonly Color32[] Cork = Ramp("#6b4a2e", "#8a6440", "#a87e52");
        private static readonly Color32[] Label = Ramp("#7A2A30", "#9a3a3e");
        private static readonly Color32[] Sauce = Ramp("#1d0e08", "#34190e", "#4d2614", "#6b3a1e");

        /// <summary>Batter → golden → brown → burnt. 0.62–0.8 is the golden window.</summary>
        public static Color32 CookColor(float d)
        {
            d = Mathf.Max(0, d);
            float v = Mathf.Min(Cook.Length - 1.001f, d < .8f ? d * 3.4f : 2.72f + (d - .8f) * 9);
            int b = (int)Math.Floor(v);
            return Mix(Cook[b], Cook[Math.Min(Cook.Length - 1, b + 1)], v - b);
        }

        // ------------------------------------------------------------------ shared sprites
        private static bool built;
        public static PixelSprite Peeler, Grater, Bundle, Pail, Shaker, Spoon, Turner, Bottle, Pan, Handle, MixBowl, Plate, Dip;
        public static PixelSprite ArrowDown, ArrowUp, ArrowRight, Hand;
        public static BowlSprites Bowl;
        public static PixelSprite CounterBg, StoveBg;

        public static void Build()
        {
            if (built)
                return;
            built = true;
            Peeler = PeelerSprite();
            Grater = GraterSprite();
            Bowl = BowlParts();
            Bundle = BundleSprite();
            Pail = PailSprite();
            Shaker = ShakerSprite();
            Spoon = SpoonSprite();
            Turner = TurnerSprite();
            Bottle = BottleSprite();
            Pan = PanSprite();
            Handle = HandleSprite();
            MixBowl = MixBowlSprite();
            Plate = PlateSprite();
            Dip = DipSprite();

            var arrow = new Dictionary<char, Color32> { { '#', Out }, { 'y', Hex("#FFC05A") } };
            ArrowDown = FromMap(new[] { "..###..", "..#y#..", "..#y#..", "..#y#..", "###y###", "#yyyyy#", ".#yyy#.", "..#y#..", "...#..." }, arrow);
            ArrowUp = FlipY(ArrowDown);
            ArrowRight = FromMap(new[] { "....#....", "....##...", "#####y#..", "#yyyyyy#.", "#yyyyyyy#", "#yyyyyy#.", "#####y#..", "....##...", "....#...." }, arrow);
            Hand = FromMap(new[]
            {
                "...##.......",
                "..#ww#......",
                "..#ww#......",
                "..#ww###....",
                "..#ww#ww##..",
                ".##ww#ww#w#.",
                "#w#wwwwww#w#",
                "#wwwwwwwwwww",
                "#wwwwwwwwww#",
                ".#wsswwwww#.",
                "..#wwwwww#..",
                "..#cccccc#..",
                "..########.."
            }, new Dictionary<char, Color32>
            {
                { '#', Out }, { 'w', Hex("#fff6e2") }, { 's', Hex("#e8d6b4") }, { 'c', Hex("#7A2A30") }
            });

            CounterBg = Background(true);
            StoveBg = Background(false);
        }

        // ------------------------------------------------------------------ potatoes
        public static PotatoShape PotatoShapeOf(int w, int h, float seed)
        {
            var s = new PotatoShape { W = w, H = h, Ins = new bool[w * h], L = new float[w * h] };
            float cx = w / 2f, cy = h / 2f, rx = w / 2f - 1.5f, ry = h / 2f - 1.5f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x + .5f - cx) / rx, ny = (y + .5f - cy) / ry, ang = Mathf.Atan2(ny, nx);
                float r = .92f + .05f * Mathf.Sin(ang * 3 + seed) + .03f * Mathf.Sin(ang * 5 + seed * 2.3f);
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                if (d > r)
                    continue;
                int i = y * w + x;
                s.Ins[i] = true;
                s.Count++;
                float q = d / r, z = Mathf.Sqrt(Mathf.Max(0, 1 - q * q));
                s.L[i] = Clamp(.16f + .6f * z - .26f * (nx / r) - .36f * (ny / r), 0, 1);
            }

            return s;
        }

        public static PixelSprite PeeledPotato(int w, int h, float seed)
        {
            PotatoShape s = PotatoShapeOf(w, h, seed);
            return Make(w, h, (x, y) =>
            {
                int i = y * w + x;
                return s.Ins[i] ? Sh(Flesh, s.L[i] + (Vn(x / 4f + 9, y / 4f) - .5f) * .14f, x, y) : None;
            }, Out);
        }

        public static PixelSprite CuttingBoard(int w, int h)
        {
            return Make(w, h, (x, y) =>
            {
                const int r = 10;
                int ix = Math.Min(x - 1, w - 2 - x), iy = Math.Min(y - 1, h - 2 - y);
                if (ix < 0 || iy < 0)
                    return None;
                if (ix < r && iy < r)
                {
                    int dx = r - ix, dy = r - iy;
                    if (dx * dx + dy * dy > r * r)
                        return None;
                }

                float hx = w - 16, hy = h / 2f - 3;
                if (Sq((x - hx) / 6) + Sq((y - hy) / 4) <= 1)
                    return None;
                if (y > h - 8)
                    return Sh(Board, y > h - 4 ? .05f : .18f, x, y);
                float l = .55f + (Vn(x / 14f, y / 2.2f) - .5f) * .35f + (Hash(x >> 3, y) - .5f) * .05f - (float)y / h * .15f;
                if (iy < 2 || ix < 2)
                    l += .18f;
                return Sh(Board, l, x, y);
            }, Out);
        }

        // ------------------------------------------------------------------ tools
        private static PixelSprite PeelerSprite() => Make(18, 32, (x, y) =>
        {
            if (y >= 1 && y <= 4 && x >= 2 && x <= 15)
            {
                if (y == 2 && x >= 4 && x <= 13)
                    return Hex("#2c3038");
                return Sh(Steel, .9f - (y - 1) * .15f, x, y);
            }

            float dA = SegDist(x + .5f, y + .5f, 3, 4, 7, 15), dB = SegDist(x + .5f, y + .5f, 15, 4, 11, 15);
            if (dA < 1.1f || dB < 1.1f)
                return Sh(Steel, .6f, x, y);
            if (y >= 14 && y <= 30 && x >= 6 && x <= 11)
            {
                if (y == 30 && (x == 6 || x == 11))
                    return None;
                return Sh(Wood, .9f - (x - 6) / 5f * .6f + (y == 14 ? .1f : 0), x, y);
            }

            return None;
        }, Out);

        private static PixelSprite GraterSprite()
        {
            const int w = 62, h = 104;
            return Make(w, h, (x, y) =>
            {
                if (y < 12)
                {
                    bool inO = x >= 19 && x <= 42 && y >= 1, inI = x >= 23 && x <= 38 && y >= 5;
                    if (inO && !inI)
                    {
                        if (y <= 2 && (x == 19 || x == 42))
                            return None;
                        return Sh(Plastic, .75f - (y - 1) / 12f * .45f, x, y);
                    }

                    return None;
                }

                float t = (y - 12f) / (h - 14), hw = 21 + t * 7, cx = 31;
                if (x + .5f < cx - hw || x + .5f > cx + hw || y > h - 2)
                    return None;
                float u = (x + .5f - (cx - hw)) / (2 * hw);
                if (u < .12f)
                    return Sh(Steel, .12f + u * 1.6f, x, y);
                if (y >= 17 && y < h - 6 && u > .17f && u < .93f)
                {
                    int row = (y - 17) / 6, ry = (y - 17) % 6, off = row % 2 * 3;
                    int v = (int)Math.Floor(x - cx + hw) + off, rx = (v % 6 + 6) % 6;
                    if ((ry == 2 || ry == 3) && rx >= 1 && rx <= 3)
                        return ry == 2 ? Hex("#1b1e24") : Hex("#3a3f48");
                    if (ry == 1 && rx >= 1 && rx <= 3)
                        return Steel[5];
                }

                float l = .36f + u * .45f + (u > .62f && u < .7f ? .25f : 0) - t * .08f;
                if (y <= 13)
                    l += .25f;
                return Sh(Steel, l, x, y);
            }, Out);
        }

        private static BowlSprites BowlParts()
        {
            const int w = 132, h = 44, cx = 66, ry0 = 8, rx = 62, rr = 7;
            bool InRim(int x, int y) => Sq((x + .5f - cx) / rx) + Sq((y + .5f - ry0) / rr) <= 1;
            bool InInner(int x, int y) => Sq((x + .5f - cx) / (rx - 4)) + Sq((y + .5f - ry0) / (rr - 2)) <= 1;
            bool InBody(int x, int y)
            {
                if (y + .5f < ry0)
                    return false;
                float t = (y + .5f - ry0) / (h - 3 - ry0);
                if (t > 1)
                    return false;
                float hw = Mathf.Max(rx * Mathf.Sqrt(Mathf.Max(0, 1 - t * t * t * t)), 24);
                return Mathf.Abs(x + .5f - cx) <= hw * (1 - t * .12f);
            }

            PixelSprite back = Make(w, h, (x, y) =>
            {
                if (InInner(x, y))
                    return Sh(Ceram, .18f + (y - ry0 + 5) / 10f * .35f, x, y);
                if (InRim(x, y))
                    return Sh(Ceram, .95f, x, y);
                if (InBody(x, y))
                {
                    float t = (y - ry0) / (float)(h - ry0);
                    if (y >= ry0 + 5 && y <= ry0 + 7)
                        return Sh(Blue, .45f + ((x >> 1) & 1) * .3f, x, y);
                    if (y == ry0 + 11 && x % 8 < 2)
                        return Blue[1];
                    return Sh(Ceram, .8f - (x - cx) / (float)rx * .25f - t * .4f, x, y);
                }

                return None;
            }, Out);
            PixelSprite front = Make(w, h, (x, y) =>
                y + .5f >= ry0 + .5f && InRim(x, y) && !InInner(x, y) ? Sh(Ceram, .92f, x, y) : None);
            return new BowlSprites { Back = back, Front = front, Ry0 = ry0 };
        }

        public static PixelSprite PileSprite(float p)
        {
            int r = Mathf.RoundToInt(8 + 44 * p), hh = Mathf.RoundToInt(2 + 15 * p), w = r * 2 + 4, h = hh + 5;
            return Make(w, h, (x, y) =>
            {
                float dx = (x + .5f - w / 2f) / r;
                if (Mathf.Abs(dx) > 1)
                    return None;
                float top = h - 3 - hh * Mathf.Pow(1 - dx * dx, .7f) - (Hash(x, 7) - .5f) * 2;
                if (y + .5f < top || y > h - 2)
                    return None;
                float l = .32f + .5f * (1 - (y - top) / Mathf.Max(1, h - 2 - top)) + (Hash(x, y) - .5f) * .5f - dx * .1f;
                return Sh(Flesh, l, x, y);
            }, Out);
        }

        private static PixelSprite BundleSprite() => Make(66, 62, (x, y) =>
        {
            const float cx = 33, cy = 40, r = 20.5f;
            float dx = x + .5f - cx, dy = y + .5f - cy, d = Mathf.Sqrt(dx * dx + Sq(dy * 1.05f));
            bool inside = d <= r, neck = false;
            if (!inside)
            {
                if (y >= 13 && y <= 21 && Mathf.Abs(x + .5f - cx) <= 3 + (21 - y) * .15f)
                {
                    inside = true;
                    neck = true;
                }

                if (y >= 2 && y < 13)
                {
                    float tl = SegDist(x + .5f, y + .5f, cx - 1, 13, cx - 10, 3), tr = SegDist(x + .5f, y + .5f, cx + 1, 13, cx + 9, 2);
                    if (tl < 2.7f || tr < 2.4f)
                    {
                        inside = true;
                        neck = true;
                    }
                }
            }

            if (!inside)
                return None;
            if (neck)
                return Sh(Cloth, .75f - (x - cx) * .05f - ((x + y) & 1) * .06f, x, y);
            float nx = dx / r, ny = dy / r, z = Mathf.Sqrt(Mathf.Max(0, 1 - nx * nx - ny * ny));
            float l = .22f + .58f * z - .25f * nx - .35f * ny, ang = Mathf.Atan2(dy, dx);
            if (dy < 6 && Mathf.Sin(ang * 9 + d * .3f) > .8f)
                l -= .24f;
            if (((x + y) & 1) == 0)
                l -= .05f;
            if (dy > 0 && Hash(x, y) < .07f)
                return Sh(Flesh, .4f + z * .4f, x, y);
            return Sh(Cloth, l, x, y);
        }, Out);

        private static PixelSprite PailSprite() => Make(44, 38, (x, y) =>
        {
            const int h = 38;
            float t = y / (float)(h - 2), hw = 20 - t * 4, cx = 22;
            if (y < 2 || y > h - 2 || Mathf.Abs(x + .5f - cx) > hw)
                return None;
            if (y < 7)
            {
                float e = Sq((x + .5f - cx) / (hw - 1)) + Sq((y - 4.5f) / 2.5f);
                if (e <= 1)
                    return y < 5 ? Hex("#16181d") : Hex("#2a2e36");
            }

            float u = (x + .5f - (cx - hw)) / (2 * hw);
            float l = .22f + u * .55f + (u > .66f && u < .74f ? .25f : 0);
            if (y == 10 || y == h - 7)
                l -= .25f;
            return Sh(Steel, l, x, y);
        }, Out);

        private static PixelSprite ShakerSprite() => Make(20, 30, (x, y) =>
        {
            const float cx = 10;
            if (y >= 1 && y <= 8)
            {
                if (Mathf.Abs(x + .5f - cx) > 7 - (y < 2 ? 1 : 0))
                    return None;
                if (y >= 3 && y <= 4 && (x == 6 || x == 10 || x == 14))
                    return Hex("#2a2a30");
                return Sh(Steel, .9f - (x - 3) / 14f * .6f, x, y);
            }

            if (y >= 9 && y <= 28)
            {
                if (Mathf.Abs(x + .5f - cx) > 8 - (y >= 27 ? 1 : 0))
                    return None;
                float u = (x - 2) / 16f;
                if (y >= 14)
                {
                    if (u < .1f || u > .9f)
                        return Sh(GlassR, .6f, x, y);
                    return Sh(Starch, .95f - u * .45f + (Hash(x, y) - .5f) * .3f, x, y);
                }

                if (u > .2f && u < .3f)
                    return Hex("#f4fbfd");
                return Sh(GlassR, .6f - u * .4f, x, y);
            }

            return None;
        }, Out);

        private static PixelSprite SpoonSprite() => Make(32, 32, (x, y) =>
        {
            float e = Sq((x + .5f - 7) / 5.5f) + Sq((y + .5f - 25) / 4.3f);
            if (e <= 1)
                return Sh(Wood, .85f - e * .45f - (x - 7) * .03f, x, y);
            float d = SegDist(x + .5f, y + .5f, 10, 22, 29, 3);
            return d < 1.6f ? Sh(Wood, .75f - d * .25f, x, y) : None;
        }, Out);

        private static PixelSprite TurnerSprite() => Make(34, 34, (x, y) =>
        {
            if (x >= 2 && x <= 15 && y >= 21 && y <= 31)
            {
                if ((x == 2 || x == 15) && (y == 21 || y == 31))
                    return None;
                if (y >= 23 && y <= 29 && (x == 6 || x == 10))
                    return Hex("#23262d");
                return Sh(Steel, .85f - (y - 21) / 10f * .5f + (x < 4 ? .1f : 0), x, y);
            }

            float d = SegDist(x + .5f, y + .5f, 12, 21, 31, 3);
            if (d < 1.7f)
                return y < 12 ? Sh(Wood, .7f - d * .2f, x, y) : Sh(Steel, .5f, x, y);
            return None;
        }, Out);

        private static PixelSprite BottleSprite()
        {
            const float cx = 9;
            Color32 Glass(int x, int y, bool oil)
            {
                float u = (x - 1) / 16f;
                if (u > .18f && u < .28f)
                    return Hex("#fff8e0");
                return oil ? Sh(OilR, .85f - u * .7f, x, y) : Sh(GlassR, .65f - u * .4f, x, y);
            }

            return Make(18, 44, (x, y) =>
            {
                if (y >= 1 && y <= 5)
                    return Mathf.Abs(x + .5f - cx) > 3 ? None : Sh(Cork, .75f - (x - 6) * .1f, x, y);
                if (y >= 6 && y <= 13)
                    return Mathf.Abs(x + .5f - cx) > 2.6f ? None : Glass(x, y, false);
                if (y >= 14 && y <= 18)
                {
                    float hw = 2.6f + (y - 13) * 1.2f;
                    return Mathf.Abs(x + .5f - cx) > hw ? None : Glass(x, y, y >= 16);
                }

                if (y >= 19 && y <= 42)
                {
                    if (Mathf.Abs(x + .5f - cx) > 7.5f)
                        return None;
                    if (y >= 26 && y <= 33)
                        return y == 26 || y == 33 ? Hex("#6b2226") : Sh(Label, .6f, x, y);
                    return Glass(x, y, true);
                }

                return None;
            }, Out);
        }

        private static PixelSprite PanSprite()
        {
            const float cx = 86, cy = 60, rxo = 80, ryo = 55, rxi = 69, ryi = 46;
            return Make(172, 130, (x, y) =>
            {
                float X = x + .5f - cx, Y = y + .5f - cy;
                float eo = Sq(X / rxo) + Sq(Y / ryo), ei = Sq(X / rxi) + Sq((Y - 2) / ryi), es = Sq(X / rxo) + Sq((Y - 9) / ryo);
                if (ei <= 1)
                    return Sh(Iron, .22f + (1 - ei) * .2f - Y / ryi * .06f + (Hash(x, y) < .03f ? .1f : 0), x, y);
                if (eo <= 1)
                {
                    float ang = Mathf.Atan2(Y, X), l = .5f + Mathf.Cos(ang + 2.2f) * .3f;
                    if (eo > .88f)
                        l += .22f;
                    return Sh(Iron, l, x, y);
                }

                if (es <= 1 && Y > 0)
                    return Sh(Iron, .14f - X / rxo * .1f, x, y);
                return None;
            }, Out);
        }

        private static PixelSprite HandleSprite() => Make(94, 18, (x, y) =>
        {
            if (x < 14)
                return y < 5 || y > 12 ? None : Sh(Iron, .55f - (y - 5) / 8f * .35f, x, y);
            if (y < 3 || y > 14)
                return None;
            if (x > 89 && (y < 5 || y > 12))
                return None;
            if (x > 80 && x < 84 && y >= 7 && y <= 10)
                return Out;
            return Sh(Wood, .8f - (y - 3) / 11f * .65f + (Vn(x / 8f, y) - .5f) * .15f, x, y);
        }, Out);

        private static PixelSprite MixBowlSprite()
        {
            const float cx = 82, cy = 52, rxo = 78, ryo = 48, rxi = 68, ryi = 40;
            return Make(164, 118, (x, y) =>
            {
                float X = x + .5f - cx, Y = y + .5f - cy;
                float eo = Sq(X / rxo) + Sq(Y / ryo), ei = Sq(X / rxi) + Sq(Y / ryi), eb = Sq(X / rxo) + Sq((Y - 13) / ryo);
                if (ei <= 1)
                    return Sh(Ceram, .3f + Y / ryi * .3f + -X / rxi * .1f, x, y);
                if (eo <= 1)
                {
                    float ang = Mathf.Atan2(Y, X), ri = Mathf.Sqrt(ei), ro = Mathf.Sqrt(eo), t = (ri - 1) / (ri - 1 + (1 - ro) + 1e-6f);
                    if (Mathf.Abs(t - .5f) < .2f && (int)Math.Floor((ang + Mathf.PI) / (Mathf.PI * 2) * 48) % 3 == 0)
                        return Sh(Blue, .55f, x, y);
                    return Sh(Ceram, .72f + Mathf.Cos(ang + 2.3f) * .2f + (t > .85f ? -.12f : 0), x, y);
                }

                if (eb <= 1 && Y > 0)
                {
                    float edge = ryo * Mathf.Sqrt(Mathf.Max(0, 1 - Sq(X / rxo)));
                    if (Mathf.Abs(Y - edge - 7) < 1.1f)
                        return Sh(Blue, .45f, x, y);
                    return Sh(Ceram, .55f - X / rxo * .25f - (Y - edge) / 13 * .2f, x, y);
                }

                return None;
            }, Out);
        }

        private static PixelSprite PlateSprite()
        {
            const float cx = 95, cy = 60, rxo = 90, ryo = 58, rxi = 64, ryi = 40;
            return Make(190, 128, (x, y) =>
            {
                float X = x + .5f - cx, Y = y + .5f - cy;
                float eo = Sq(X / rxo) + Sq(Y / ryo), ei = Sq(X / rxi) + Sq((Y - 1) / ryi), eb = Sq(X / rxo) + Sq((Y - 5) / ryo);
                if (ei <= 1)
                    return Sh(Ceram, .62f + (1 - ei) * .25f - Y / ryi * .08f, x, y);
                if (eo <= 1)
                {
                    float ang = Mathf.Atan2(Y, X), ri = Mathf.Sqrt(ei), ro = Mathf.Sqrt(eo), t = (ri - 1) / (ri - 1 + (1 - ro) + 1e-6f);
                    if (t > .35f && t < .62f)
                        return (int)Math.Floor((ang + Mathf.PI) / (Mathf.PI * 2) * 40) % 2 == 0 ? Sh(Blue, .6f, x, y) : Sh(Ceram, .85f, x, y);
                    if (t < .12f)
                        return Sh(Ceram, .45f, x, y);
                    return Sh(Ceram, .8f + Mathf.Cos(ang + 2.3f) * .15f, x, y);
                }

                return eb <= 1 && Y > 0 ? Sh(Ceram, .35f - X / rxo * .15f, x, y) : None;
            }, Out);
        }

        private static PixelSprite DipSprite() => Make(44, 30, (x, y) =>
        {
            float X = x + .5f - 22, Y = y + .5f - 12;
            float eo = Sq(X / 20) + Sq(Y / 10), ei = Sq(X / 16) + Sq((Y - 1) / 7), eb = Sq(X / 20) + Sq((Y - 6) / 10);
            if (ei <= 1)
                return Hash(x, y) < .06f ? Hex("#f3e2b0") : Sh(Sauce, .35f + (1 - ei) * .3f - Y / 7 * .15f, x, y);
            if (eo <= 1)
                return Sh(Ceram, .85f + (X < 0 ? .05f : -.1f), x, y);
            return eb <= 1 && Y > 0 ? Sh(Blue, .45f - X / 20 * .2f, x, y) : None;
        }, Out);

        public static PixelSprite MedalSprite(int tier)
        {
            Color32[] disc = tier switch
            {
                0 => Ramp("#7a4a12", "#b7791f", "#e3a83a", "#ffd66b", "#fff1b3"),
                1 => Ramp("#4b5260", "#7b8494", "#aab2c0", "#d6dce6", "#f4f7fb"),
                2 => Ramp("#4a2412", "#7c4220", "#a8622f", "#cf8a4e", "#ecb27d"),
                _ => Ramp("#2a2224", "#3d3335", "#554849", "#6d5f60", "#857676")
            };
            return Make(24, 32, (x, y) =>
            {
                if (y < 12)
                {
                    float l = SegDist(x + .5f, y + .5f, 7, 0, 11, 12), r = SegDist(x + .5f, y + .5f, 17, 0, 13, 12);
                    if (l < 2.6f)
                        return Sh(Ribbon, .6f, x, y);
                    return r < 2.6f ? Sh(Ribbon, .3f, x, y) : None;
                }

                float X = x + .5f - 12, Y = y + .5f - 21, d = Mathf.Sqrt(X * X + Y * Y);
                if (d > 9.5f)
                    return None;
                if (d > 7.6f)
                    return Sh(disc, .5f - (X + Y) * .04f, x, y);
                bool star = (Mathf.Abs(X) < 1.2f && Mathf.Abs(Y) < 4) || (Mathf.Abs(Y) < 1.2f && Mathf.Abs(X) < 4);
                return star ? disc[4] : Sh(disc, .75f - (X + Y) * .05f, x, y);
            }, Out);
        }

        // ------------------------------------------------------------------ backgrounds
        private static Color32 WallPixel(int x, int y)
        {
            if (y >= 30 && y <= 33)
                return y == 33 ? Sh(Wood, .15f, x, y) : Sh(Wood, .62f - (y - 30) * .15f + (Vn(x / 9f, y) - .5f) * .2f, x, y);
            if (y == 34 || y == 35)
                return Brick[0];
            int row = y / 8, off = row % 2 * 8, bx = ((x + off) % 16 + 16) % 16, by = y % 8;
            if (by == 7 || bx == 15)
                return Brick[0];
            float light = Clamp(1 - Mathf.Sqrt(Sq(x - 250) + Sq(y - 8)) / 240, 0, 1);
            float l = .2f + light * .55f + (Hash((x + off) >> 4, row) - .5f) * .25f + (by == 0 ? .12f : 0) + (bx == 0 ? .06f : 0) - (by == 6 ? .08f : 0);
            return Sh(BrickTail, l, x, y);
        }

        private static readonly Color32[] BrickTail = Tail(Brick);
        private static readonly Color32[] WoodTail = Tail(Wood);
        private static readonly Color32[] StoneTail = Tail(Stone);

        private static PixelSprite Background(bool counter)
        {
            PixelSprite bg = Make(PotatoKitchen.ArtW, PotatoKitchen.ArtH, (x, y) =>
            {
                if (y < 62)
                    return WallPixel(x, y);
                if (counter)
                {
                    if (y < 64)
                        return Sh(Wood, .95f, x, y);
                    int p = (y - 64) / 20, yy = (y - 64) % 20;
                    if (yy == 0)
                        return Wood[0];
                    int seam = ((x + p * 53) % 96 + 96) % 96;
                    if (seam == 0)
                        return Wood[0];
                    float l = .5f + (Vn(x / 18f + p * 7, yy / 3f) - .5f) * .34f + (Hash(p, (x + p * 53) / 96) - .5f) * .16f - (y - 64) / 116f * .3f + (yy == 1 ? .12f : 0);
                    return Sh(WoodTail, l, x, y);
                }

                if (y < 65)
                    return Sh(Stone, .9f, x, y);
                int tx = x / 30, ty = (y - 65) / 22, mx = x % 30, my = (y - 65) % 22;
                if (mx == 0 || my == 0)
                    return Stone[0];
                float sl = .32f + (Hash(tx, ty) - .5f) * .22f + (Vn(x / 5f, y / 5f) - .5f) * .18f - (y - 65) / 115f * .2f + (my == 1 ? .1f : 0);
                return Sh(StoneTail, sl, x, y);
            });

            // Shelf props: jars and a copper pot.
            void Put(PixelSprite s, int px, int py)
            {
                for (int y = 0; y < s.H; y++)
                for (int x = 0; x < s.W; x++)
                {
                    Color32 c = s.Px[y * s.W + x];
                    if (c.a != 0)
                        bg.Px[(py + y) * bg.W + px + x] = c;
                }
            }

            void Rect(int px, int py, int w, int h, Color32 c)
            {
                for (int y = py; y < py + h; y++)
                for (int x = px; x < px + w; x++)
                    bg.Px[y * bg.W + x] = c;
            }

            Put(Jar(14, 16, Ceram, true), 30, 14);
            Put(Jar(11, 13, Ceram, true), 46, 17);
            Put(Jar(16, 14, Ramp("#5a2a14", "#8a4420", "#b8622e", "#d98a4a", "#f0b070"), false), 206, 16);
            Put(Jar(12, 15, Ramp("#3d4a3a", "#56684f", "#7a8f6a", "#9fb38a"), false), 226, 15);
            for (int i = 0; i < 5; i++)
            {
                Rect(100, 2 + i * 5, 3, 4, Hex("#b8302a"));
                Rect(100, 2 + i * 5, 1, 2, Hex("#e0574a"));
            }

            for (int i = 0; i < 4; i++)
            {
                Rect(114, 4 + i * 6, 5, 5, Hex("#e9e1cf"));
                Rect(118, 5 + i * 6, 1, 4, Hex("#b9ae98"));
            }

            return bg;
        }

        private static PixelSprite Jar(int w, int h, Color32[] ramp, bool band) => Make(w, h, (i, j) =>
        {
            float cx = w / 2f;
            if (j < 2)
                return Mathf.Abs(i + .5f - cx) > w / 2f - 3 ? None : Sh(Wood, .6f, i, j);
            float hw = w / 2f - 1 - (j < 4 ? 1 : 0);
            if (Mathf.Abs(i + .5f - cx) > hw || j > h - 2)
                return None;
            float u = (i + .5f - (cx - hw)) / (2 * hw);
            if (band && j >= h / 2f - 1 && j <= h / 2f + 1)
                return Sh(Blue, .5f + u * .3f, i, j);
            return Sh(ramp, .25f + u * .6f + (u > .62f && u < .7f ? .2f : 0), i, j);
        }, Out);
    }
}
