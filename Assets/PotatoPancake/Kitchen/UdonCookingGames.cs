using System;
using System.Collections.Generic;
using UnityEngine;
using static FantasyClinaryArk.Px;

namespace FantasyClinaryArk
{
    internal sealed class UdonBatch
    {
        public float Water = 600;
        public float Sauce = 75;
        public int Pepper;
    }

    /// <summary>
    /// Props for the 겐로쿠 우동 stage, painted like the 감자전 kitchen: dithered ramps lit from the top left,
    /// a little noise for texture and the shared dark outline.
    /// </summary>
    internal static class UdonArt
    {
        public static readonly Color32[] White = Ramp("#8e9a7c", "#b9c4a0", "#dbe3c2", "#eef3df", "#fbfcf3");
        public static readonly Color32[] Leaf = Ramp("#22401f", "#315a27", "#457a32", "#5d9a3e", "#7eb856", "#a4d276");
        public static readonly Color32[] Char = Ramp("#1a110d", "#3a2416", "#6b4322", "#a0702f", "#d0a058");
        public static readonly Color32[] Broth = Ramp("#5a2a0e", "#7f3f14", "#a3561c", "#c27228", "#d9913c", "#ecb562");
        public static readonly Color32[] WaterR = Ramp("#2f4e57", "#467079", "#62919a", "#86b2b6", "#b2d3d2", "#dcefec");
        public static readonly Color32[] Tsuyu = Ramp("#140806", "#2a140b", "#432213", "#63351d");
        public static readonly Color32[] Noodle = Ramp("#a8885a", "#c9ab78", "#e2c995", "#f1e0b4", "#faf0d4", "#fffbee");
        public static readonly Color32[] PepperR = Ramp("#0f0b0b", "#1f1919", "#312a2a", "#463d3c");
        private static readonly Color32[] Red = Ramp("#5a1e23", "#7A2A30", "#9a3a3e", "#c0574f");
        private static readonly Color32[] Cap = Ramp("#24365e", "#34508a", "#4f72b4", "#7c9bd6");
        private static readonly Color32[] Indigo = Ramp("#141a33", "#1f2a4d", "#2c3f73", "#3f5aa0", "#5f7cc0");

        /// <summary>The pot sits on the burner, centred where the minigames test for taps.</summary>
        public const float PotX = 147, PotY = 98;
        private const float InnerRx = 58.5f, InnerRy = 35.5f;

        /// <summary>The whole 대파 on the board: one segment per piece it gets cut into.</summary>
        public const float StalkX = 101;
        public const int Seg = 24;

        private static PixelSprite pot, bowl, knife, waterBottle, tsuyuBottle, shaker, block, arrowLeft;
        public static PixelSprite Knife => knife ??= KnifeSprite();
        public static PixelSprite WaterBottle => waterBottle ??= BottleSprite(false);
        public static PixelSprite TsuyuBottle => tsuyuBottle ??= BottleSprite(true);
        public static PixelSprite PepperShaker => shaker ??= ShakerSprite();
        public static PixelSprite NoodleBlock => block ??= BlockSprite();
        public static PixelSprite ArrowLeft => arrowLeft ??= Mirror(KitchenArt.ArrowRight);
        private static PixelSprite PotSprite => pot ??= PotBody();
        private static PixelSprite BowlSprite => bowl ??= BowlBody();

        // ------------------------------------------------------------------ 대파
        private static readonly Dictionary<int, PixelSprite> pieces = new Dictionary<int, PixelSprite>();
        private static readonly Dictionary<int, PixelSprite> stalks = new Dictionary<int, PixelSprite>();

        /// <summary>A chunk of 대파 on its side, cut face to the right. Roast browns the underside and adds grill marks.</summary>
        public static PixelSprite Piece(int length, float roast)
        {
            int step = Mathf.Clamp(Mathf.RoundToInt(roast * 10), 0, 18), key = length * 100 + step;
            if (pieces.TryGetValue(key, out PixelSprite cached))
                return cached;
            float c = step / 10f, fx = length - 1.5f;
            const float cy = 6.5f, r = 5;
            PixelSprite s = Make(length + 2, 13, (x, y) =>
            {
                float v = (y + .5f - cy) / r, px = x + .5f;
                if (Mathf.Abs(v) > 1 || px < 1)
                    return None;
                if (px < 3.5f && Sq((px - 3.5f) / 2.5f) + v * v > 1)
                    return None;
                bool face = px >= fx;
                if (face && Sq((px - fx) / 2.5f) + v * v > 1)
                    return None;
                Color32 col = face ? CutFace(px - fx, v) : Sh(White, .8f - v * .4f - v * v * .28f - (Hash(3, y) < .3f ? .06f : 0), x, y);
                if (c <= 0)
                    return col;
                float burn = c * (.3f + .4f * Mathf.Max(0, v));
                if (!face && c > .5f && (x + y / 2) % 7 < 2)
                    burn += .25f * (c - .3f);
                if (Hash(x, y + 50) < c * .08f)
                    burn += .3f;
                burn = Clamp(burn, 0, .95f);
                return Mix(col, Sh(Char, 1 - burn * burn * 1.1f - burn * .2f, x, y), Mathf.Min(.92f, burn * 1.3f));
            }, KitchenArt.Out);
            pieces[key] = s;
            return s;
        }

        private static Color32 CutFace(float dx, float v)
        {
            float d = Mathf.Sqrt(Sq(dx / 2.5f) + v * v);
            return d < .28f ? Hex("#c3d394") : ((int)(d * 3.4f) & 1) == 0 ? Hex("#f3f6e2") : Hex("#d2dead");
        }

        /// <summary>
        /// Segments from..to of the long 대파 as one outlined piece: hairy root at the far left,
        /// white turning green toward the leaves on the right, rings showing where it has been cut.
        /// </summary>
        private static PixelSprite StalkPiece(int from, int to, bool cutLeft, bool cutRight)
        {
            int key = from * 1000 + to * 100 + (cutLeft ? 10 : 0) + (cutRight ? 1 : 0);
            if (stalks.TryGetValue(key, out PixelSprite cached))
                return cached;
            int root = from == 0 ? 7 : 0, leaves = to == 3 ? 36 : 0, body = (to - from + 1) * Seg;
            const float cy = 10.5f, r = 5;
            float fx = body - 2.5f;
            PixelSprite s = Make(root + body + leaves + 2, 21, (x, y) =>
            {
                float bx = x - 1 - root + .5f, v = (y + .5f - cy) / r;
                if (bx < 0)
                {
                    for (int k = 0; k < 5; k++)
                        if (SegDist(x + .5f, y + .5f, 1 + root, cy + (k - 2) * 1.2f, 1 + k % 2, cy + (k - 2) * 3.4f) < .6f)
                            return Hex(k % 2 == 0 ? "#e6dcb6" : "#cbbf94");
                    return None;
                }

                if (bx >= body)
                {
                    if (leaves == 0)
                        return None;
                    float t = (bx - body) / leaves;
                    for (int k = 0; k < 3; k++)
                    {
                        float d = SegDist(x + .5f, y + .5f, root + body - 1, cy + (k - 1) * 2, root + body + leaves - 1 - k * 5, cy + (k - 1) * 7.5f);
                        if (d < (4.2f - t * 2.6f) * .5f + .4f)
                            return Sh(Leaf, .78f - t * .25f - (k == 1 ? 0 : .14f) + (d < .8f ? .12f : 0), x, y);
                    }

                    return None;
                }

                if (Mathf.Abs(v) > 1)
                    return None;
                if ((cutLeft || from == 0) && bx < 2.5f && Sq((bx - 2.5f) / 2.5f) + v * v > 1)
                    return None;
                if (cutRight && bx >= fx)
                    return Sq((bx - fx) / 2.5f) + v * v > 1 ? None : CutFace(bx - fx, v);
                float green = Clamp((from * Seg + bx - 62) / 34, 0, 1), l = .8f - v * .4f - v * v * .28f - (Hash(3, y) < .3f ? .06f : 0);
                return Mix(Sh(White, l, x, y), Sh(Leaf, l + .08f, x, y), green);
            }, KitchenArt.Out);
            stalks[key] = s;
            return s;
        }

        /// <summary>The 대파 on the board after <paramref name="cuts"/> cuts; finished pieces slide a little apart.</summary>
        public static void Stalk(PixelCanvas a, float y, int cuts, float x0 = StalkX)
        {
            a.FillRect(x0 - 4, y + 6, 4 * Seg + 38, 2, Rgba(40, 20, 10, .2f));
            for (int j = 0; j < cuts && j < 3; j++)
                Put(a, StalkPiece(j, j, j > 0, true), j, x0 - (cuts - j) * 3, y);
            Put(a, StalkPiece(Mathf.Min(cuts, 3), 3, cuts > 0, false), Mathf.Min(cuts, 3), x0, y);
        }

        private static void Put(PixelCanvas a, PixelSprite s, int from, float x0, float y) =>
            a.Blit(s, x0 + from * Seg - 1 - (from == 0 ? 7 : 0), y - 10.5f);

        // ------------------------------------------------------------------ pot and broth
        private static PixelSprite PotBody()
        {
            const float cx = 80, cy = 46, rxo = 66, ryo = 42, rxi = InnerRx, ryi = InnerRy;
            return Make(160, 108, (x, y) =>
            {
                float X = x + .5f - cx, Y = y + .5f - cy;
                float eo = Sq(X / rxo) + Sq(Y / ryo), ei = Sq(X / rxi) + Sq((Y - 1) / ryi), eb = Sq(X / rxo) + Sq((Y - 18) / ryo);
                if (ei <= 1)
                {
                    float far = Clamp(-(Y - 1) / ryi, 0, 1);
                    return Sh(KitchenArt.Steel, .1f + (1 - ei) * .16f + far * .3f + (Hash(x, y) < .02f ? .1f : 0), x, y);
                }

                if (eo <= 1)
                {
                    float l = .66f + Mathf.Cos(Mathf.Atan2(Y, X) + 2.3f) * .25f;
                    if (eo > .9f)
                        l -= .15f;
                    else if (ei < 1.12f)
                        l -= .22f;
                    return Sh(KitchenArt.Steel, l, x, y);
                }

                if (eb <= 1 && Y > 0)
                {
                    float u = X / rxo, edge = 18 + ryo * Mathf.Sqrt(Mathf.Max(0, 1 - u * u));
                    float l = .5f - u * .3f + (u > -.62f && u < -.5f ? .3f : 0) + (u > .35f && u < .4f ? .14f : 0);
                    if (Y > edge - 3)
                        l -= .22f;
                    if (Mathf.Abs(Y - 26) < .6f)
                        l -= .12f;
                    return Sh(KitchenArt.Steel, l, x, y);
                }

                float hx = Mathf.Abs(X) - (rxo + 4), hy = Y - 6;
                if (Mathf.Abs(X) > rxo - 4 && Sq(hx / 8) + Sq(hy / 5) <= 1 && Sq(hx / 4.5f) + Sq(hy / 2) > 1)
                    return Sh(KitchenArt.Iron, .7f - hy * .1f, x, y);
                return None;
            }, KitchenArt.Out);
        }

        private static float Level(float water) => Mathf.Min(1, Clamp(water / 600, 0, 1.2f));
        private static float SurfaceY(float water) => PotY + 1 + (1 - Level(water)) * 9;
        private static float SurfaceRx(float water) => 58 * (.88f + .12f * Level(water));
        private static float SurfaceRy(float water) => 35 * (.86f + .14f * Level(water));

        /// <summary>Top edge of the liquid (or the pot floor) at column x, where pours and pepper land.</summary>
        public static float SurfaceTop(float water, float x)
        {
            float u = Clamp((x - PotX) / SurfaceRx(water), -.95f, .95f);
            return SurfaceY(water) - SurfaceRy(water) * Mathf.Sqrt(1 - u * u);
        }

        /// <summary>Plain water turning to tsuyu-brown broth; well past 100 ml it darkens further.</summary>
        private static Color32[] LiquidRamp(float sauce)
        {
            var ramp = new Color32[WaterR.Length];
            float t = Mathf.Pow(Clamp(sauce / 75, 0, 1), .55f), dark = 1 - Clamp((sauce - 100) / 80, 0, .35f);
            for (int i = 0; i < ramp.Length; i++)
                ramp[i] = Scale(Mix(WaterR[i], Broth[i], t), dark);
            return ramp;
        }

        private static void Surface(PixelCanvas a, float cx, float cy, float rx, float ry, Color32[] ramp, float time,
            float clipX, float clipY, float clipRx, float clipRy)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= cy + ry; y++)
            for (int x = Mathf.FloorToInt(cx - rx); x <= cx + rx; x++)
            {
                float nx = (x + .5f - cx) / rx, ny = (y + .5f - cy) / ry, e = nx * nx + ny * ny;
                if (e > 1 || Sq((x + .5f - clipX) / clipRx) + Sq((y + .5f - clipY) / clipRy) > 1)
                    continue;
                float l = .55f - nx * .1f - ny * .16f + (Vn(x / 6f + time * .7f, y / 3f) - .5f) * .18f;
                if (e > .5f && e < .68f && nx < -.15f && ny < -.05f)
                    l += .22f;
                if (e > .86f)
                    l -= .14f;
                a.Blend(x, y, Sh(ramp, l, x, y));
            }
        }

        private static void Bubbles(PixelCanvas a, float cx, float cy, float rx, float ry, Color32 light, float time, float heat)
        {
            int count = Mathf.RoundToInt(6 * heat);
            for (int i = 0; i < count; i++)
            {
                float clock = time * .8f + i * .37f, phase = clock % 1;
                int cycle = Mathf.FloorToInt(clock);
                float bx = Mathf.Round(cx + (Hash(i, cycle) - .5f) * rx * 1.4f), by = Mathf.Round(cy + (Hash(cycle, i + 9) - .5f) * ry * 1.2f);
                if (Sq((bx - cx) / rx) + Sq((by - cy) / ry) > .75f)
                    continue;
                if (phase < .6f)
                    a.FillRect(bx, by, 1, 1, light, .75f);
                else if (phase < .82f)
                {
                    a.FillRect(bx - 1, by, 1, 1, light, .8f);
                    a.FillRect(bx + 1, by, 1, 1, light, .8f);
                    a.FillRect(bx, by - 1, 1, 1, light, .8f);
                    a.FillRect(bx, by + 1, 1, 1, light, .5f);
                }
            }
        }

        private static readonly Vector2[] Floating = { new Vector2(-30, -14), new Vector2(-8, 6), new Vector2(16, -8), new Vector2(32, 12) };

        /// <summary>
        /// The stock pot on a lit burner with the roasted 대파 in it.
        /// <paramref name="noodles"/> is -1 before the noodles go in, then 0 (a packed block) to 1 (loosened).
        /// </summary>
        public static void Pot(PixelCanvas a, float water, float sauce, int pepper, float noodles, float time)
        {
            Stove.Flames(a, time);
            a.Blit(PotSprite, PotX - 80, PotY - 46);
            float sx = PotX, sy = SurfaceY(water), rx = SurfaceRx(water), ry = SurfaceRy(water);
            bool wet = water > 1;
            Color32[] ramp = LiquidRamp(sauce);
            if (wet)
            {
                Surface(a, sx, sy, rx, ry, ramp, time, PotX, PotY + 1, InnerRx, InnerRy);
                Bubbles(a, sx, sy, rx, ry, ramp[ramp.Length - 1], time, Level(water));
            }

            if (noodles >= 0)
            {
                Strands(a, sx - 2, sy + 2, noodles, time, rx - 4, ry - 3);
                if (noodles < .9f)
                    a.Blit(NoodleBlock, sx - 23, sy - 15 + noodles * 4, Clamp(1 - noodles * 1.1f, 0, 1));
            }

            for (int i = 0; i < Floating.Length; i++)
            {
                float bob = wet ? Mathf.Round(Mathf.Sin(time * 1.8f + i * 1.9f)) : 0;
                float px = sx + Floating[i].x * rx / 58, py = sy + Floating[i].y * ry / 35 + bob;
                a.Blit(Piece(18, .95f), px - 10, py - 6);
                if (wet)
                    a.FillRect(px - 8, py + 3, 16, 2, ramp[2], .55f);
            }

            if (wet)
                Flecks(a, sx, sy, rx, ry, pepper * 6, time);
        }

        /// <summary>
        /// A nest of thick udon strands: overlapping arcs, each a shaded tube with a dark seam under it.
        /// Packed tight at 0, drifting loose and wider at 1; clipped to the liquid.
        /// </summary>
        private static void Strands(PixelCanvas a, float cx, float cy, float loose, float time, float rx, float ry, int strands = 12)
        {
            for (int s = 0; s < strands; s++)
            {
                float ox = (Hash(s, 1) - .5f) * rx * (.45f + .4f * loose), oy = (Hash(s, 2) - .5f) * ry * (.4f + .35f * loose);
                float arx = rx * (.22f + .3f * Hash(s, 3)) * (.75f + .35f * loose), ary = arx * .55f;
                float from = Hash(s, 4) * Mathf.PI * 2 + time * .15f * loose * (s % 2 == 0 ? 1 : -1), span = 2.4f + Hash(s, 5) * 2.4f;
                int steps = Mathf.CeilToInt(span * arx * 1.2f);
                for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i <= steps; i++)
                {
                    float t = from + span * i / steps;
                    float px = Mathf.Round(cx + ox + Mathf.Cos(t) * arx), py = Mathf.Round(cy + oy + Mathf.Sin(t) * ary);
                    for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int x = (int)px + dx, y = (int)py + dy + (pass == 0 ? 1 : 0);
                        if (dx * dx + dy * dy > (pass == 0 ? 5 : 2) || Sq((x + .5f - cx) / rx) + Sq((y + .5f - cy) / ry) > 1)
                            continue;
                        if (pass == 0)
                            a.Blend(x, y, Noodle[0], .5f);
                        else
                            a.Blend(x, y, Sh(Noodle, .78f - dy * .26f - dx * .06f, x, y));
                    }
                }
            }
        }

        /// <summary>Ground pepper floating on the surface, slowly drifting round.</summary>
        private static void Flecks(PixelCanvas a, float cx, float cy, float rx, float ry, int count, float time)
        {
            for (int i = 0; i < Mathf.Min(60, count); i++)
            {
                float angle = Hash(i, 3) * Mathf.PI * 2 + time * .08f, dist = Mathf.Sqrt(Hash(i, 5)) * .88f;
                float px = Mathf.Round(cx + Mathf.Cos(angle) * dist * rx), py = Mathf.Round(cy + Mathf.Sin(angle) * dist * ry);
                Color32 col = PepperR[Hash(i, 9) < .5f ? 0 : 2];
                a.FillRect(px, py, Hash(i, 11) < .2f ? 2 : 1, 1, col);
            }
        }

        /// <summary>Steam off the pot, more of it the hotter and fuller it is.</summary>
        public static void Steam(KitchenContext k, float dt, float amount)
        {
            if (amount <= 0 || Rnd() > dt * 14 * amount)
                return;
            k.Parts.Emit(PotX + (Rnd() - .5f) * 90, PotY - 8 + (Rnd() - .5f) * 26, (Rnd() - .5f) * 5, -10 - Rnd() * 8,
                Rgba(255, 255, 255, .28f), 1.8f, -3, 2, float.NaN, false, true);
        }

        // ------------------------------------------------------------------ tools and ingredients
        private static PixelSprite KnifeSprite() => Make(13, 42, (x, y) =>
        {
            if (y <= 13)
            {
                if (x < 3 || x > 9 || (y == 0 && (x == 3 || x == 9)))
                    return None;
                if (x == 6 && (y == 4 || y == 9))
                    return KitchenArt.Steel[4];
                return Sh(KitchenArt.Wood, .85f - (x - 3) / 6f * .6f + (Vn(x, y / 3f) - .5f) * .15f, x, y);
            }

            if (y <= 16)
                return x < 2 || x > 10 ? None : Sh(KitchenArt.Steel, .75f - (x - 2) * .05f, x, y);
            float right = y < 33 ? 11 : 11 - (y - 33) * 1.2f;
            if (x < 1 || x > right || y > 41)
                return None;
            if (x <= 2)
                return KitchenArt.Steel[5];
            if (x >= right - 1)
                return KitchenArt.Steel[1];
            return Sh(KitchenArt.Steel, .45f + (x - 1) / 10f * .35f + (y == 17 ? .1f : 0), x, y);
        }, KitchenArt.Out);

        private static PixelSprite BottleSprite(bool tsuyu) => Make(22, 54, (x, y) =>
        {
            const float cx = 11;
            float ax = Mathf.Abs(x + .5f - cx);
            float hw = y <= 6 ? 3.5f : y <= 12 ? 2.6f : y <= 19 ? 2.6f + (y - 12) * 1.05f : 9.5f;
            if (y < 1 || y > 52 || ax > hw || (y >= 51 && ax > hw - 1))
                return None;
            float u = (x + .5f - (cx - hw)) / (2 * hw);
            if (y <= 6)
                return Sh(tsuyu ? Red : Cap, .85f - u * .6f + (y == 1 ? .1f : 0), x, y);
            bool label = tsuyu ? y >= 27 && y <= 42 : y >= 30 && y <= 40;
            if (label && u > .06f && u < .94f)
            {
                if (tsuyu)
                {
                    if (y <= 28 || y >= 41)
                        return Sh(Red, .5f + (1 - u) * .3f, x, y);
                    if (x >= 9 && x <= 13 && y >= 31 && y <= 38 && Hash(x, y) < .5f)
                        return KitchenArt.Out;
                }
                else if (Mathf.Abs(y - (35 + Mathf.Sin(x * .9f) * 1.5f)) < .7f)
                    return Sh(KitchenArt.Blue, .6f, x, y);

                return Sh(KitchenArt.Ceram, .95f - u * .45f, x, y);
            }

            if (u > .16f && u < .27f && y > 8)
                return Hex("#f6fbfc");
            if (tsuyu)
                return Sh(Tsuyu, .9f - u * .75f, x, y);
            return y >= 22 ? Sh(WaterR, .95f - u * .6f, x, y) : Sh(KitchenArt.GlassR, .9f - u * .5f, x, y);
        }, KitchenArt.Out);

        private static PixelSprite ShakerSprite() => Make(20, 30, (x, y) =>
        {
            const float cx = 10;
            if (y >= 1 && y <= 8)
            {
                if (Mathf.Abs(x + .5f - cx) > 7 - (y < 2 ? 1 : 0))
                    return None;
                if (y >= 3 && y <= 4 && (x == 6 || x == 10 || x == 14))
                    return Hex("#2a2a30");
                return Sh(KitchenArt.Steel, .9f - (x - 3) / 14f * .6f, x, y);
            }

            if (y < 9 || y > 28 || Mathf.Abs(x + .5f - cx) > 8 - (y >= 27 ? 1 : 0))
                return None;
            float u = (x - 2) / 16f;
            if (u < .1f || u > .9f)
                return Sh(KitchenArt.GlassR, .5f, x, y);
            if (y < 12)
                return u > .2f && u < .3f ? Hex("#f4fbfd") : Sh(KitchenArt.GlassR, .6f - u * .4f, x, y);
            if (Hash(x, y) < .12f)
                return Hex("#6b5f58");
            return u > .2f && u < .27f ? Sh(PepperR, .99f, x, y) : Sh(PepperR, .8f - u * .6f + (Hash(x + 7, y) - .5f) * .4f, x, y);
        }, KitchenArt.Out);

        /// <summary>A packed block of udon, strands folded back and forth.</summary>
        private static PixelSprite BlockSprite() => Make(46, 30, (x, y) =>
        {
            int ix = Math.Min(x - 1, 44 - x), iy = Math.Min(y - 1, 28 - y);
            if (ix < 0 || iy < 0 || (ix < 5 && iy < 5 && Sq(5 - ix) + Sq(5 - iy) > 25))
                return None;
            float wave = Mathf.Sin((y + Mathf.Sin(x * .32f) * 1.8f) * 1.25f);
            float l = wave < -.8f ? .12f : .62f + wave * .22f - y / 30f * .22f + (iy < 2 ? .12f : 0);
            return Sh(Noodle, l, x, y);
        }, KitchenArt.Out);

        private static PixelSprite Mirror(PixelSprite s)
        {
            var m = new PixelSprite(s.W, s.H);
            for (int y = 0; y < s.H; y++)
            for (int x = 0; x < s.W; x++)
                m.Px[y * s.W + x] = s.Px[y * s.W + s.W - 1 - x];
            return m;
        }

        // ------------------------------------------------------------------ the finished bowl
        private static PixelSprite BowlBody()
        {
            const float cx = 85, cy = 50, rxo = 80, ryo = 48, rxi = 72, ryi = 41;
            return Make(170, 126, (x, y) =>
            {
                float X = x + .5f - cx, Y = y + .5f - cy;
                float eo = Sq(X / rxo) + Sq(Y / ryo), ei = Sq(X / rxi) + Sq((Y - 1) / ryi), eb = Sq(X / (rxo - 2)) + Sq((Y - 24) / ryo);
                if (ei <= 1)
                    return Sh(KitchenArt.Ceram, .3f + Y / ryi * .3f - X / rxi * .1f, x, y);
                if (eo <= 1)
                    return Sh(KitchenArt.Ceram, .82f + Mathf.Cos(Mathf.Atan2(Y, X) + 2.3f) * .15f - (eo > .9f ? .12f : 0), x, y);
                if (eb <= 1 && Y > 0)
                {
                    float u = X / rxo;
                    if (Hash(x, y) < .035f)
                        return KitchenArt.Ceram[3];
                    if (Y < 9)
                        return Sh(Indigo, .25f - u * .1f, x, y);
                    return Sh(Indigo, .62f - u * .35f + (u > -.6f && u < -.48f ? .25f : 0) - (Y - 9) / 70f * .25f, x, y);
                }

                if (Mathf.Abs(X) < 32 && Y > 60 && Y < 76)
                    return Sh(KitchenArt.Ceram, .5f - X / 32 * .25f - (Y > 72 ? .15f : 0), x, y);
                return None;
            }, KitchenArt.Out);
        }

        public static void Bowl(PixelCanvas art, float x, float y, float pepper = 8)
        {
            art.Ellipse(x + 4, y + 70, 70, 12, Rgba(0, 0, 0, .35f));
            art.Blit(BowlSprite, x - 85, y - 50);
            Color32[] broth = LiquidRamp(75);
            Surface(art, x, y + 3, 70, 37, broth, 0, x, y + 3, 70, 37);
            Strands(art, x - 4, y + 4, .7f, 0, 62, 31, 20);
            art.Blit(Piece(20, 1), x + 10, y - 24);
            art.Blit(Piece(20, .9f), x - 42, y - 20);
            art.Blit(Piece(20, 1.05f), x + 22, y + 8);
            Flecks(art, x, y + 3, 66, 34, Mathf.RoundToInt(pepper * 5), 0);
        }

        public static PixelSprite StorySprite(bool plated)
        {
            var art = new PixelCanvas(PotatoKitchen.ArtW, PotatoKitchen.ArtH);
            Array.Copy(KitchenArt.CounterBg.Px, art.Buf, art.Buf.Length);
            if (plated)
                Bowl(art, 160, 89);
            else
            {
                art.Blit(KitchenArt.CuttingBoard(176, 90), 72, 48);
                Stalk(art, 80, 0, 104);
                Stalk(art, 112, 0, 98);
            }

            var sprite = new PixelSprite(art.W, art.H);
            Array.Copy(art.Buf, sprite.Px, art.Buf.Length);
            if (art.Texture != null)
                UnityEngine.Object.Destroy(art.Texture);
            return sprite;
        }
    }

    internal abstract class UdonGame : CookingGame
    {
        protected UdonGame(KitchenContext kitchen) : base(kitchen) { }

        protected void Finish(int score, string note, Sfx sound = Sfx.Ok)
        {
            Done = true;
            Score = score;
            Note = note;
            K.Play(sound);
        }

        protected static bool Near(Vector2 point, float x, float y, float rx, float ry) =>
            Sq((point.x - x) / rx) + Sq((point.y - y) / ry) <= 1;
    }

    // ====================================================================== 1. slice the 대파
    internal sealed class SliceScallionGame : UdonGame
    {
        private const int Cuts = 3;
        private const float StalkY = 98;
        private PixelSprite board;
        private int cuts;
        private bool armed;
        private float chop, chopX;
        private static float CutX(int index) => UdonArt.StalkX + (index + 1) * UdonArt.Seg;
        public SliceScallionGame(KitchenContext k) : base(k) { }

        public override void Update(float dt, bool active) => chop = Mathf.Max(0, chop - dt);
        public override void Down(Vector2 p) => armed = cuts < Cuts && Mathf.Abs(p.x - CutX(cuts)) <= 12 && p.y < 91;

        public override void Move(Vector2 p)
        {
            if (armed && p.y >= 107 && Mathf.Abs(p.x - CutX(cuts)) <= 16)
                Submit();
        }

        public override void Up(Vector2 p) => armed = false;

        public override void Submit()
        {
            if (Done)
                return;
            armed = false;
            chopX = CutX(cuts);
            chop = .22f;
            K.Play(Sfx.Peel);
            for (int i = 0; i < 7; i++)
                K.Parts.Emit(chopX + (R - .5f) * 4, StalkY - 2, (R - .5f) * 50, -40 - R * 30,
                    R < .5f ? UdonArt.White[3] : UdonArt.Leaf[4], .6f, 220, 1, StalkY + 4 + R * 4, true);
            if (++cuts == Cuts)
                Finish(100, "대파가 큼직하게 잘렸어요");
        }

        public override (int, string) Timeout() => (cuts * 25, "표시선을 위에서 아래로 그어 보세요");
        public override void Gauges(List<Gauge> list) => list.Add(Gauge.Meter("대파 손질", cuts / 3f, $"{cuts} / {Cuts}"));
        public override string Measure() => cuts == Cuts ? "대파 4토막 · 한 토막 3~4cm" : $"대파 {cuts + 1}토막";

        public override void Art(PixelCanvas a)
        {
            board ??= KitchenArt.CuttingBoard(176, 90);
            a.Blit(board, 72, 48);
            UdonArt.Stalk(a, StalkY, cuts);
            if (cuts < Cuts && chop <= 0)
            {
                float x = CutX(cuts);
                for (int y = 72; y < 122; y += 6)
                    if ((y / 6 + Mathf.FloorToInt(K.Time * 6)) % 3 != 0)
                        a.FillRect(x, y, 1, 3, Hex("#FFC05A"));
            }
        }

        public override void ArtTop(PixelCanvas a)
        {
            if (chop > 0)
                a.Blit(UdonArt.Knife, chopX - 2, StalkY + 8 - 41);
            else if (Done)
                return;
            else if (K.ShowTool)
                a.Blit(UdonArt.Knife, Mathf.Round(Ptr.x - 2), Mathf.Round(Ptr.y - 41));
            else
                a.Blit(UdonArt.Knife, CutX(cuts) - 2, 80 - 41 + Bob(4, 2));
        }
    }

    // ====================================================================== 2. roast the 대파 dry
    internal sealed class RoastScallionGame : UdonGame
    {
        private const float Low = .81f, High = 1.35f;
        private readonly float[] heat = new float[4], hop = new float[4];
        private int turned, score;
        private static float X(int i) => 117 + i % 2 * 52;
        private static float Y(int i) => 81 + i / 2 * 34;
        public RoastScallionGame(KitchenContext k) : base(k) { }

        public override void Update(float dt, bool active)
        {
            for (int i = 0; i < hop.Length; i++)
                hop[i] = Mathf.Max(0, hop[i] - dt);
            if (!active)
                return;
            for (int i = turned; i < heat.Length; i++)
                heat[i] += dt / (2.35f + i * .75f);
            for (int i = 0; i < heat.Length; i++)
            {
                float h = i < turned ? heat[i] * .4f : heat[i];
                if (h > .45f && R < dt * 5 * h)
                    K.Parts.Emit(X(i) + (R - .5f) * 24, Y(i) - 4, (R - .5f) * 6, -14 - R * 8,
                        h > High ? Rgba(60, 50, 50, .5f) : Rgba(235, 230, 220, .35f), 1.1f, -5, 2, float.NaN, false, true);
            }
        }

        public override void Down(Vector2 p)
        {
            if (turned < heat.Length && Near(p, X(turned), Y(turned), 23, 15))
                Submit();
        }

        public override void Submit()
        {
            if (Done)
                return;
            float roast = heat[turned];
            score += roast >= Low && roast <= High ? 100 : roast >= .55f && roast < 1.7f ? 70 : 38;
            hop[turned] = .3f;
            K.Play(Sfx.Flip);
            for (int i = 0; i < 6; i++)
                K.Parts.Emit(X(turned) + (R - .5f) * 20, Y(turned), (R - .5f) * 40, -30 - R * 20, Hex(R < .5f ? "#ffd166" : "#ff9a4a"), .4f, 120);
            if (++turned == heat.Length)
                Finish(score / heat.Length, score >= 360 ? "기름 없이 구우니 대파 향이 확 올라와요" : "겉이 살짝 그을렸을 때 뒤집으면 향이 좋아요");
        }

        public override (int, string) Timeout() => (score / heat.Length, "살짝 그을린 대파를 하나씩 눌러 뒤집어 보세요");
        public override string Measure() => turned == heat.Length ? "기름 0ml · 대파 4토막 겉만 그을림" : null;

        public override void Gauges(List<Gauge> list)
        {
            float roast = turned < heat.Length ? heat[turned] : 1;
            Gauge gauge = Gauge.Meter("대파 굽기", roast / 1.6f, $"{turned} / {heat.Length}개 뒤집음");
            gauge.Segmented = false;
            gauge.ZoneMin = Low / 1.6f;
            gauge.ZoneMax = High / 1.6f;
            gauge.ZoneLabel = "향이 올라오는 구간";
            gauge.ZoneColor = Hex("#79b28c", .35f);
            list.Add(gauge);
        }

        public override Callout? Shout =>
            Done || turned >= heat.Length ? (Callout?)null
            : heat[turned] > High ? new Callout("타기 직전!", "#ff9a7a")
            : heat[turned] >= Low ? new Callout("지금 뒤집어요!", "#a8dcb0")
            : (Callout?)null;

        public override void Art(PixelCanvas a)
        {
            Stove.Draw(a, K.Time);
            for (int i = 0; i < heat.Length; i++)
            {
                float lift = hop[i] > 0 ? Mathf.Round(Mathf.Sin(hop[i] / .3f * Mathf.PI) * 7) : 0;
                a.Blit(UdonArt.Piece(28, heat[i]), X(i) - 15, Y(i) - 6.5f - lift);
            }
        }

        public override void ArtTop(PixelCanvas a)
        {
            if (!Done && turned < heat.Length)
                a.Blit(KitchenArt.ArrowDown, X(turned) - 3, Y(turned) - 22 + Bob(6, 2));
        }
    }

    // ====================================================================== 3. water and tsuyu
    internal sealed class UdonBrothGame : UdonGame
    {
        private bool saucePhase, pouring;
        private float water, sauce, tilt, keyPour, sound;
        private float BottleX => saucePhase ? 252 : 232;
        private bool Flowing => (pouring || keyPour > 0) && tilt > .8f;
        public UdonBrothGame(KitchenContext k) : base(k) { }

        public override void Update(float dt, bool active)
        {
            keyPour = Mathf.Max(0, keyPour - dt);
            tilt = Clamp(tilt + (pouring || keyPour > 0 ? dt * 4.5f : -dt * 3.5f), 0, 1);
            if (!active)
                return;
            UdonArt.Steam(K, dt, water / 600 * .6f);
            if (!Flowing)
                return;
            if (pouring)
            {
                if (saucePhase)
                    sauce = Mathf.Min(140, sauce + dt * 48);
                else
                    water = Mathf.Min(760, water + dt * 315);
            }

            sound -= dt;
            if (sound <= 0)
            {
                K.Play(Sfx.Pour);
                sound = .12f;
            }

            Color32[] ramp = saucePhase ? UdonArt.Tsuyu : UdonArt.WaterR;
            if (R < .7f)
                K.Parts.Emit(179 + (R - .5f) * 6, UdonArt.SurfaceTop(water, 179), (R - .5f) * 30, -18 - R * 18, ramp[ramp.Length - 1], .3f, 160);
        }

        public override void Down(Vector2 p)
        {
            if (!Done && Near(p, BottleX, 106, 34, 36))
                pouring = true;
        }

        public override void Up(Vector2 p)
        {
            if (!pouring || Done)
                return;
            pouring = false;
            if (!saucePhase && water >= 450)
                saucePhase = true;
            else if (saucePhase && sauce >= 35)
                Complete();
        }

        public override void Submit()
        {
            if (Done)
                return;
            keyPour = .4f;
            if (!saucePhase)
            {
                water += 150;
                saucePhase = water >= 600;
            }
            else
            {
                sauce += 25;
                if (sauce == 75)
                    Complete();
            }
        }

        private void Complete()
        {
            int waterScore = Mathf.Abs(water - 600) <= 45 ? 100 : 65;
            int sauceScore = sauce >= 50 && sauce <= 100 ? 100 : 60;
            string note = waterScore < 100 ? "물은 600ml 눈금에 맞춰 보세요"
                : sauceScore < 100 ? "쯔유는 50~100ml 사이에서 멈춰 보세요" : "쯔유로 국물 간을 딱 맞췄어요";
            Finish((waterScore + sauceScore) / 2, note);
        }

        public override (int, string) Timeout() => (saucePhase ? 45 : 20, "물부터 붓고 쯔유로 간을 맞춰 보세요");

        public override string Measure()
        {
            K.Udon.Water = water;
            K.Udon.Sauce = sauce;
            return $"물 {water:0}ml · 쯔유 {sauce:0}ml";
        }

        public override void Gauges(List<Gauge> list)
        {
            list.Add(MeasuringGauge("물", water, 760, 555, 645, "600ml 목표"));
            list.Add(MeasuringGauge("쯔유", sauce, 140, 50, 100, "50~100ml"));
        }

        public override Callout? Shout => pouring && (saucePhase ? sauce >= 50 && sauce <= 100 : water >= 555 && water <= 645)
            ? new Callout("지금 떼요!", "#a8dcb0")
            : (Callout?)null;

        private static Gauge MeasuringGauge(string name, float value, float capacity, float low, float high, string zone)
        {
            Gauge gauge = Gauge.Meter(name, value / capacity, $"{value:0}ml");
            gauge.Segmented = false;
            gauge.ZoneMin = low / capacity;
            gauge.ZoneMax = high / capacity;
            gauge.ZoneColor = Hex("#79b28c", .35f);
            gauge.ZoneLabel = zone;
            return gauge;
        }

        public override void Art(PixelCanvas a) => UdonArt.Pot(a, water, sauce, 0, -1, K.Time);

        public override void ArtTop(PixelCanvas a)
        {
            float e = tilt * tilt * (3 - 2 * tilt);
            float bx = Lerp(BottleX, 204, e), by = Lerp(106, 46, e), r = Lerp(0, -1.95f, e);
            a.BlitRotated(saucePhase ? UdonArt.TsuyuBottle : UdonArt.WaterBottle, Mathf.Round(bx), Mathf.Round(by), r, -11, -27);
            if (Flowing)
            {
                Color32[] ramp = saucePhase ? UdonArt.Tsuyu : UdonArt.WaterR;
                float mx = Mathf.Round(bx + Mathf.Sin(r) * 27), my = Mathf.Round(by - Mathf.Cos(r) * 27);
                int frame = Mathf.FloorToInt(K.Time * 20);
                for (float y = my + 1; y < UdonArt.SurfaceTop(water, mx); y++)
                {
                    float wobble = Hash((int)y, frame) > .75f ? 1 : 0;
                    a.FillRect(mx - 1 + wobble, y, 2, 1, Sh(ramp, .7f, (int)mx, (int)y));
                    a.FillRect(mx + 1 + wobble, y, 1, 1, ramp[ramp.Length - 1], .6f);
                }
            }
            else if (!pouring && tilt <= 0 && !Done)
                a.Blit(KitchenArt.ArrowDown, BottleX - 3, 66 + Bob(6, 2));
        }
    }

    // ====================================================================== 4. plenty of pepper
    internal sealed class UdonPepperGame : UdonGame
    {
        private const int Target = 8;
        private int taps;
        private float shake;
        public UdonPepperGame(KitchenContext k) : base(k) { }

        public override void Update(float dt, bool active)
        {
            shake = Mathf.Max(0, shake - dt);
            if (active)
                UdonArt.Steam(K, dt, .6f);
        }

        public override void Down(Vector2 p)
        {
            if (Near(p, 234, 100, 30, 42))
                Submit();
        }

        public override void Submit()
        {
            if (Done)
                return;
            taps++;
            K.Udon.Pepper = taps;
            shake = .2f;
            K.Play(Sfx.Salt);
            for (int i = 0; i < 10; i++)
            {
                float x = 197 + R * 6;
                K.Parts.Emit(x, 80 + R * 5, -10 - R * 25, 10 + R * 15, UdonArt.PepperR[(int)(R * 3)], 1.2f, 140, 1,
                    UdonArt.SurfaceTop(K.Udon.Water, x - 25) + 2 + R * 30, true);
            }

            if (taps >= Target)
                Finish(100, "후추 향이 확 올라왔어요. 이제 면을 넣어요");
        }

        public override (int, string) Timeout() => (taps * 10, "후추가 이 국물의 핵심이에요");
        public override void Gauges(List<Gauge> list) => list.Add(Gauge.Meter("후추 향", taps / (float)Target, $"{taps} / {Target}"));
        public override string Measure() => $"후추 {taps}번 톡톡";

        public override void Art(PixelCanvas a) => UdonArt.Pot(a, K.Udon.Water, K.Udon.Sauce, taps, -1, K.Time);

        public override void ArtTop(PixelCanvas a)
        {
            if (shake > 0)
                a.BlitRotated(UdonArt.PepperShaker, 212, 74, -2.2f + Mathf.Sin(shake * 60) * .25f, -10, -15);
            else
                a.Blit(UdonArt.PepperShaker, 224, 82 + Bob(5, 1));
            if (taps == 0 && shake <= 0)
                a.Blit(KitchenArt.ArrowDown, 231, 68 + Bob(6, 2));
        }
    }

    // ====================================================================== 5. noodles in, then into the bowl
    internal sealed class UdonNoodleGame : UdonGame
    {
        private bool grabbing, added;
        private float cooking;
        public UdonNoodleGame(KitchenContext k) : base(k) { }

        public override void Update(float dt, bool active)
        {
            if (!active)
                return;
            UdonArt.Steam(K, dt, .7f);
            if (added)
                cooking += dt;
        }

        public override void Down(Vector2 p)
        {
            if (!added)
                grabbing = Near(p, 236, 110, 32, 28);
            else if (Near(p, 147, 98, 62, 43))
                Submit();
        }

        public override void Up(Vector2 p)
        {
            if (grabbing && Near(p, 147, 98, 60, 40))
                Submit();
            grabbing = false;
        }

        public override void Submit()
        {
            if (Done)
                return;
            if (!added)
            {
                added = true;
                grabbing = false;
                K.Play(Sfx.Plop);
                for (int i = 0; i < 16; i++)
                    K.Parts.Emit(UdonArt.PotX + (R - .5f) * 40, UdonArt.PotY - 8, (R - .5f) * 60, -40 - R * 30, UdonArt.Broth[4], .5f, 200);
            }
            else if (cooking >= 1.4f)
                Finish(cooking <= 5 ? 100 : 70, cooking <= 5 ? "면이 풀리자마자 뜨거울 때 담았어요" : "면이 풀리면 바로 담아 보세요");
        }

        public override (int, string) Timeout() => (added ? 55 : 20, "면이 풀리면 냄비를 눌러 담아 보세요");
        public override string Measure() => added ? "우동면 1인분(약 200g)" : null;

        public override void Gauges(List<Gauge> list) =>
            list.Add(Gauge.Meter("면 익히기", cooking / 1.4f, !added ? "면 넣기" : cooking < 1.4f ? "익는 중" : "그릇에 담기"));

        public override Callout? Shout => added && !Done && cooking >= 1.4f && cooking <= 5
            ? new Callout("지금 담아요!", "#a8dcb0")
            : (Callout?)null;

        public override void Art(PixelCanvas a) =>
            UdonArt.Pot(a, K.Udon.Water, K.Udon.Sauce, K.Udon.Pepper, added ? Clamp(cooking / 1.4f, 0, 1) : -1, K.Time);

        public override void ArtTop(PixelCanvas a)
        {
            if (!added)
            {
                Vector2 pos = grabbing ? Ptr : new Vector2(238, 109 + Bob(4, 1));
                a.Blit(UdonArt.NoodleBlock, Mathf.Round(pos.x - 23), Mathf.Round(pos.y - 15));
                if (!grabbing)
                    a.Blit(UdonArt.ArrowLeft, 202 + Bob(6, 2), 104);
            }
            else if (cooking >= 1.4f && !Done)
                a.Blit(KitchenArt.ArrowDown, UdonArt.PotX - 3, 44 + Bob(6, 2));
        }
    }
}
