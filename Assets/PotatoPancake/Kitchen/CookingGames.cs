using System;
using System.Collections.Generic;
using UnityEngine;
using static FantasyClinaryArk.Px;
using static FantasyClinaryArk.KitchenArt;

namespace FantasyClinaryArk
{
    /// <summary>State the minigames share with the kitchen: pointer, clock, effects and the pancake being made.</summary>
    internal sealed class KitchenContext
    {
        public readonly Particles Parts = new Particles();
        public KitchenSound Sound;
        public float Time;
        public float Shake;
        public Vector2 Pointer = new Vector2(-99, -99);
        public bool PointerDown;
        public bool Hovering;
        public byte[] PancakeMask;
        public PixelSprite FinalPancake;

        public bool ShowTool => Pointer.x > -50 && (PointerDown || Hovering);
        public void Play(Sfx effect) => Sound?.Play(effect);
    }

    internal sealed class Particles
    {
        private struct P
        {
            public float X, Y, Vx, Vy, Life, Max, G, Stick;
            public Color32 Col;
            public int Size;
            public bool Die, Fade;
        }

        private readonly List<P> list = new List<P>();

        public void Clear() => list.Clear();

        public void Emit(float x, float y, float vx, float vy, Color32 col, float life, float g = 60, int size = 1,
            float stick = float.NaN, bool die = false, bool fade = false)
        {
            list.Add(new P { X = x, Y = y, Vx = vx, Vy = vy, Col = col, Life = life, Max = life, G = g, Size = size, Stick = stick, Die = die, Fade = fade });
            if (list.Count > 900)
                list.RemoveRange(0, list.Count - 900);
        }

        public void Update(float dt)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                P p = list[i];
                p.Life -= dt;
                if (p.G != 0 || p.Vx != 0 || p.Vy != 0)
                {
                    p.Vy += p.G * dt;
                    p.X += p.Vx * dt;
                    p.Y += p.Vy * dt;
                }

                if (!float.IsNaN(p.Stick) && p.Y >= p.Stick)
                {
                    p.Y = p.Stick;
                    p.Vx = p.Vy = p.G = 0;
                    if (p.Die)
                        p.Life = Mathf.Min(p.Life, .04f);
                }

                if (p.Life <= 0)
                    list.RemoveAt(i);
                else
                    list[i] = p;
            }
        }

        public void Draw(PixelCanvas a)
        {
            foreach (P p in list)
            {
                float alpha = p.Fade ? Clamp(p.Life / p.Max * 1.6f, 0, 1) : 1;
                a.FillRect(Mathf.Round(p.X), Mathf.Round(p.Y), p.Size, p.Size, p.Col, alpha);
            }
        }
    }

    /// <summary>One bar in the side panel. Fill/zone/marker values are 0–1 along the bar; negative means hidden.</summary>
    internal struct Gauge
    {
        public string Label;
        public string Value;
        public float Fill;
        public Color32 FillColor;
        public bool Segmented;
        public float ZoneMin, ZoneMax;
        public Color32 ZoneColor;
        public string ZoneLabel;
        public float Marker;
        public bool MarkerAlert;
        public bool CookGradient;

        public static Gauge Meter(string label, float fill, string value = null, Color32? color = null) => new Gauge
        {
            Label = label,
            Fill = fill,
            Value = value ?? Mathf.RoundToInt(Clamp(fill, 0, 1) * 100) + "%",
            FillColor = color ?? Hex("#E0A43A"),
            Segmented = true,
            ZoneMin = -1,
            Marker = -1
        };
    }

    internal readonly struct Callout
    {
        public readonly string Text;
        public readonly Color32 Color;
        public readonly int Size;

        public Callout(string text, string color, int size = 40)
        {
            Text = text;
            Color = Hex(color);
            Size = size;
        }
    }

    internal abstract class CookingGame
    {
        protected readonly KitchenContext K;
        public bool Done;
        public int Score;
        public string Note = "";

        protected CookingGame(KitchenContext context) => K = context;

        protected Vector2 Ptr => K.Pointer;

        public abstract void Update(float dt, bool active);
        public abstract void Art(PixelCanvas a);
        public virtual void ArtTop(PixelCanvas a) { }
        public virtual void Down(Vector2 p) { }
        public virtual void Move(Vector2 p) { }
        public virtual void Up(Vector2 p) { }
        /// <summary>Keyboard / gamepad confirm: a simpler way through the same step.</summary>
        public virtual void Submit() { }
        public abstract (int score, string note) Timeout();
        public abstract void Gauges(List<Gauge> list);
        public virtual Callout? Shout => null;

        protected float Bob(float speed, float amount, float offset = 0) => Mathf.Round(Mathf.Sin(K.Time * speed + offset) * amount);

        public static int TimeScore(float t, float fast, float slow) =>
            Mathf.RoundToInt(t <= fast ? 100 : Clamp(100 - (t - fast) / (slow - fast) * 30, 70, 100));

        protected static float R => Rnd();
    }

    // ====================================================================== 1. peel
    internal sealed class PeelGame : CookingGame
    {
        private const int W = 100, H = 66, X0 = 110, Y0 = 60;
        private static readonly int[,] Eyes = { { 22, 20 }, { 60, 14 }, { 78, 38 }, { 40, 47 }, { 30, 32 }, { 66, 51 }, { 86, 24 } };
        private readonly PotatoShape shape = PotatoShapeOf(W, H, 1.3f);
        private readonly byte[] peel = new byte[W * H];
        private readonly PixelSprite board = CuttingBoard(184, 108);
        private readonly int[] colX = new int[5], colCount = new int[5];
        private PixelSprite sprite;
        private bool dirty = true;
        private Vector2? prev;
        private int peeled;
        private float t, sound;

        public PeelGame(KitchenContext k) : base(k)
        {
            float[] at = { .2f, .35f, .5f, .65f, .8f };
            for (int c = 0; c < at.Length; c++)
            {
                colX[c] = Mathf.RoundToInt(at[c] * W);
                for (int y = 0; y < H; y++)
                for (int x = colX[c] - 6; x <= colX[c] + 6; x++)
                    if (shape.Ins[y * W + x])
                        colCount[c]++;
            }

            Render();
        }

        private float Frac => (float)peeled / shape.Count;

        private void Render()
        {
            sprite = Make(W, H, (x, y) =>
            {
                int i = y * W + x;
                if (!shape.Ins[i])
                    return None;
                if (peel[i] != 0)
                {
                    bool edge = (x > 0 && shape.Ins[i - 1] && peel[i - 1] == 0) || (x < W - 1 && shape.Ins[i + 1] && peel[i + 1] == 0) ||
                                (y > 0 && shape.Ins[i - W] && peel[i - W] == 0) || (y < H - 1 && shape.Ins[i + W] && peel[i + W] == 0);
                    return edge ? Cut : Sh(Flesh, shape.L[i] + (Vn(x / 4f + 9, y / 4f) - .5f) * .14f, x, y);
                }

                for (int e = 0; e < Eyes.GetLength(0); e++)
                {
                    float d = Mathf.Sqrt(Sq(x - Eyes[e, 0]) + Sq(y - Eyes[e, 1]));
                    if (d < 1.3f)
                        return Eye;
                    if (d < 2.3f)
                        return Sh(Skin, shape.L[i] - .28f, x, y);
                }

                return Sh(Skin, shape.L[i] + (Vn(x / 3f, y / 3f) - .5f) * .22f - (Hash(x, y) < .05f ? .2f : 0), x, y);
            }, Out);
            dirty = false;
        }

        public override void Update(float dt, bool active)
        {
            sound -= dt;
            if (active)
                t += dt;
            if (dirty)
                Render();
        }

        private int Stamp(float bx, float by)
        {
            int count = 0;
            for (int yy = Mathf.FloorToInt(by - 3); yy <= by + 3; yy++)
            for (int xx = Mathf.FloorToInt(bx - 7); xx <= bx + 7; xx++)
            {
                if (xx < 0 || yy < 0 || xx >= W || yy >= H)
                    continue;
                if (Sq((xx + .5f - bx) / 7) + Sq((yy + .5f - by) / 3.2f) > 1)
                    continue;
                int i = yy * W + xx;
                if (shape.Ins[i] && peel[i] == 0)
                {
                    peel[i] = 1;
                    peeled++;
                    count++;
                }
            }

            return count;
        }

        public override void Down(Vector2 p) => prev = p;

        public override void Move(Vector2 p)
        {
            if (prev == null)
            {
                prev = p;
                return;
            }

            Vector2 from = prev.Value;
            float dx = p.x - from.x, dy = p.y - from.y;
            if (dy > .1f)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(dx * dx + dy * dy)));
                int got = 0;
                for (int s = 1; s <= steps; s++)
                    got += Stamp(from.x + dx * s / steps - X0, from.y + dy * s / steps - Y0);
                if (got > 0)
                    Peeled(p);
            }

            prev = p;
            CheckDone();
        }

        private void Peeled(Vector2 p)
        {
            dirty = true;
            if (R < .6f)
                K.Parts.Emit(p.x + (R - .5f) * 8, p.y + 1, (R - .5f) * 40, -15, Skin[2 + (int)(R * 3)], 4, 140, 2, Y0 + H + 6 + R * 24);
            if (sound <= 0)
            {
                K.Play(Sfx.Peel);
                sound = .07f;
            }
        }

        private void CheckDone()
        {
            if (Done || Frac < .93f)
                return;
            Done = true;
            Score = TimeScore(t, 9, 20);
            Note = t <= 9 ? "껍질이 깔끔하게 벗겨졌어요" : "다 벗겼어요. 조금만 더 빠르게!";
        }

        public override void Up(Vector2 p) => prev = null;

        public override void Submit()
        {
            // Peel the next strip that still has skin, top to bottom.
            for (int c = 0; c < colX.Length; c++)
            {
                if (Left(c) <= colCount[c] * .25f)
                    continue;
                for (float y = 0; y < H; y += 1)
                for (int o = -9; o <= 9; o += 6)
                    Stamp(colX[c] + o, y);
                Peeled(new Vector2(X0 + colX[c], Y0 + H * .7f));
                break;
            }

            // Pick up stray specks once every strip is done.
            bool stripsDone = true;
            for (int c = 0; c < colX.Length; c++)
                stripsDone &= Left(c) <= colCount[c] * .25f;
            if (Frac < .93f && stripsDone)
                for (int i = 0; i < peel.Length; i++)
                    if (shape.Ins[i] && peel[i] == 0)
                    {
                        peel[i] = 1;
                        peeled++;
                    }

            dirty = true;
            CheckDone();
        }

        private int Left(int c)
        {
            int left = 0;
            for (int y = 0; y < H; y++)
            for (int x = colX[c] - 6; x <= colX[c] + 6; x++)
            {
                int i = y * W + x;
                if (shape.Ins[i] && peel[i] == 0)
                    left++;
            }

            return left;
        }

        public override (int, string) Timeout() => (Mathf.RoundToInt(Frac * 65), "껍질이 군데군데 남았어요");

        public override void Art(PixelCanvas a)
        {
            a.Blit(board, 68, 42);
            a.Ellipse(160, 128, 52, 6, Rgba(60, 30, 20, .35f));
            a.Blit(sprite, X0, Y0);
            if (Done)
                return;
            for (int c = 0; c < colX.Length; c++)
                if (Left(c) > colCount[c] * .25f)
                    a.Blit(ArrowDown, X0 + colX[c] - 3, Y0 - 14 + Bob(6, 2, colX[c]));
        }

        public override void ArtTop(PixelCanvas a)
        {
            if (K.ShowTool)
                a.Blit(Peeler, Mathf.Round(Ptr.x - 9), Mathf.Round(Ptr.y - 3));
        }

        public override void Gauges(List<Gauge> list) => list.Add(Gauge.Meter("벗긴 껍질", Frac / .93f));
    }

    // ====================================================================== 2. grate
    internal sealed class GrateGame : CookingGame
    {
        private const int N = 12, GraterX = 129, GraterY = 24, BowlX = 94, BowlY = 112;
        private readonly PixelSprite[] sizes = new PixelSprite[7];
        private PixelSprite pile;
        private int strokes, zone, pileLevel = -1;
        private float px = 172, py = 70, t, sound;
        private bool holding;

        public GrateGame(KitchenContext k) : base(k)
        {
            for (int i = 0; i <= 6; i++)
            {
                float s = 1 - i / 6f * .6f;
                sizes[i] = PeeledPotato(Mathf.RoundToInt(46 * s) + 3, Mathf.RoundToInt(34 * s) + 3, 2.1f);
            }
        }

        private float Prog => (float)strokes / N;

        public override void Update(float dt, bool active)
        {
            sound -= dt;
            if (active)
                t += dt;
            int level = Mathf.RoundToInt(Prog * 30);
            if (level != pileLevel)
            {
                pileLevel = level;
                pile = level > 0 ? PileSprite(level / 30f) : null;
            }
        }

        public override void Down(Vector2 p)
        {
            holding = true;
            MoveTo(p);
        }

        public override void Move(Vector2 p) => MoveTo(p);
        public override void Up(Vector2 p) => holding = false;

        private void MoveTo(Vector2 p)
        {
            float nx = Clamp(p.x, 144, 176), ny = Clamp(p.y, 44, 114), moved = Mathf.Abs(ny - py);
            px = nx;
            py = ny;
            if (!holding || Done)
                return;
            int z = ny < 60 ? -1 : ny > 98 ? 1 : 0;
            if (z != 0 && z != zone)
            {
                if (zone != 0)
                    Stroke();
                zone = z;
            }

            if (moved > 1.2f)
            {
                if (sound <= 0)
                {
                    K.Play(Sfx.Grate);
                    sound = .09f;
                }

                if (R < .7f)
                    K.Parts.Emit(nx + (R - .5f) * 10, ny + 6, (R - .5f) * 20, 20, Flesh[2 + (int)(R * 3)], 1.2f, 220, 1, 116 + R * 3, true);
            }
        }

        private void Stroke()
        {
            strokes++;
            for (int i = 0; i < 8; i++)
                K.Parts.Emit(px + (R - .5f) * 12, py + 8, (R - .5f) * 30, 10, Flesh[2 + (int)(R * 3)], 1.2f, 240, 1, 116 + R * 3, true);
            if (strokes < N)
                return;
            Done = true;
            Score = TimeScore(t, 6.5f, 12);
            Note = t <= 6.5f ? "곱게 잘 갈렸어요" : "다 갈았어요. 리듬을 조금 더 빠르게!";
        }

        public override void Submit()
        {
            if (Done)
                return;
            py = py < 80 ? 108 : 50;
            K.Play(Sfx.Grate);
            Stroke();
        }

        public override (int, string) Timeout() => (Mathf.RoundToInt(Prog * 60), "감자가 덜 갈렸어요");

        public override void Art(PixelCanvas a)
        {
            a.Blit(Bowl.Back, BowlX, BowlY);
            if (pile != null)
                a.Blit(pile, 160 - pile.W / 2, BowlY + Bowl.Ry0 - pile.H + 3);
            a.Blit(Grater, GraterX, GraterY);
        }

        public override void ArtTop(PixelCanvas a)
        {
            a.Blit(Bowl.Front, BowlX, BowlY);
            if (!Done)
            {
                PixelSprite s = sizes[Mathf.Min(6, Mathf.FloorToInt(Prog * 6.99f))];
                a.Blit(s, Mathf.Round(px - s.W / 2f), Mathf.Round(py - s.H / 2f));
            }

            if (!holding && !Done)
            {
                float bob = Bob(5, 4);
                a.Blit(ArrowDown, 203, 80 + bob);
                a.Blit(ArrowUp, 203, 80 + bob - 13);
            }
        }

        public override void Gauges(List<Gauge> list) => list.Add(Gauge.Meter("갈린 감자", Prog, $"{strokes} / {N}"));
    }

    // ====================================================================== 3. squeeze
    internal sealed class SqueezeGame : CookingGame
    {
        private const int BowlX = 94, BowlY = 112;
        private float amount, squash, drop, t;
        private bool holding;

        public SqueezeGame(KitchenContext k) : base(k) { }

        public override void Update(float dt, bool active)
        {
            if (active)
            {
                t += dt;
                if (holding)
                {
                    amount = Mathf.Min(1.03f, amount + dt * (.2f + .4f * amount));
                    drop -= dt;
                    if (drop <= 0)
                    {
                        drop = .03f + .25f * (1 - amount);
                        K.Parts.Emit(160 + (R - .5f) * 16, 106, 0, 30, Hex(R < .5f ? "#a9cfc8" : "#d6ece6"), 1, 200, R < .4f ? 2 : 1, 118, true);
                        if (R < .4f)
                            K.Play(Sfx.Drip);
                    }

                    if (amount >= 1.03f)
                        Release(true);
                }
            }

            squash = Lerp(squash, holding ? 1 : 0, .25f);
        }

        public override void Down(Vector2 p)
        {
            if (!Done)
                holding = true;
        }

        public override void Up(Vector2 p)
        {
            if (holding)
                Release(false);
        }

        public override void Submit()
        {
            if (holding)
                Release(false);
            else if (!Done)
                holding = true;
        }

        private void Release(bool forced)
        {
            holding = false;
            float v = amount;
            if (v < .25f && !forced)
                return;
            Done = true;
            if (v >= .72f && v <= .9f)
            {
                Score = Mathf.RoundToInt(100 - Mathf.Abs(v - .81f) * 110);
                Note = "물기가 딱 알맞게 빠졌어요";
            }
            else if (v < .72f)
            {
                Score = Mathf.RoundToInt(40 + (v - .25f) / .47f * 40);
                Note = "물기가 덜 빠졌어요. 조금 더 꾹!";
            }
            else
            {
                Score = Mathf.RoundToInt(Clamp(78 - (v - .9f) * 260, 45, 78));
                Note = "너무 세게 짜서 감자가 뭉개졌어요";
            }
        }

        public override (int, string) Timeout() => (30, "면포를 끝까지 짜지 못했어요");

        public override void Art(PixelCanvas a)
        {
            a.Blit(Bowl.Back, BowlX, BowlY);
            float f = Clamp(amount, 0, 1);
            if (f > .02f)
            {
                a.Ellipse(160, BowlY + Bowl.Ry0 + 1, 54 * (.35f + .65f * f), 4 * (.4f + .6f * f), Hex("#93bcb5"));
                a.Ellipse(156, BowlY + Bowl.Ry0, 30 * (.35f + .65f * f), 1.5f, Hex("#cfe6e0"));
            }

            float jitter = holding ? Mathf.Round((R - .5f) * 1.5f) : 0;
            float w = 66 * (1 + .14f * squash), h = 62 * (1 - .16f * squash);
            a.BlitScaled(Bundle, Mathf.Round(160 - w / 2 + jitter), Mathf.Round(46 + 62 - h), w, h);
            if (!holding)
                return;
            Color32 strain = Hex("#fff6e2");
            foreach (int s in new[] { -1, 1 })
            {
                float bx = 160 + s * (w / 2 + 6);
                a.FillRect(bx, 76, 2, 6, strain);
                a.FillRect(bx + s * 4, 70, 2, 6, strain);
                a.FillRect(bx + s * 3, 82, 2, 5, strain);
            }
        }

        public override void ArtTop(PixelCanvas a) => a.Blit(Bowl.Front, BowlX, BowlY);

        public override void Gauges(List<Gauge> list) => list.Add(new Gauge
        {
            Label = "짜낸 물",
            Value = Mathf.RoundToInt(Clamp(amount, 0, 1) * 100) + "%",
            Fill = Clamp(amount, 0, 1),
            FillColor = Hex(amount > .9f ? "#d86a5a" : amount >= .72f ? "#8fd0a0" : "#7fb5d6"),
            ZoneMin = .72f,
            ZoneMax = .9f,
            ZoneColor = Hex("#689c7c", .55f),
            ZoneLabel = "딱 좋아요",
            Marker = -1
        });

        public override Callout? Shout => holding && amount >= .72f && amount <= .9f
            ? new Callout("지금 떼요!", "#a8dcb0")
            : (Callout?)null;
    }

    // ====================================================================== 4. pour off the clear water
    internal sealed class PourGame : CookingGame
    {
        private const float Rx = 46, Ry = 30, T0 = 6, MaxTilt = 85 * Mathf.Deg2Rad;
        private static readonly Vector2 P = new Vector2(132, 100), Pivot = new Vector2(0, 14);
        private const int RenderW = 176, RenderH = 160;
        private readonly float[] pts;
        private readonly int n;
        private readonly float[] sy, sorted;
        private readonly float v0, goal, slide;
        private float volume, th, starch = T0, t, sound, warn, grabX, level = 1e9f, keyX;
        private bool grab, keyTilt;
        private Vector2 rim;

        public PourGame(KitchenContext k) : base(k)
        {
            var list = new List<float>();
            for (int ly = 0; ly < Ry; ly++)
            for (int lx = -(int)Rx; lx < Rx; lx++)
            {
                float x = lx + .5f, y = ly + .5f;
                if (Inside(x, y) && !InStarch(x, y, T0))
                {
                    list.Add(x);
                    list.Add(y);
                }
            }

            pts = list.ToArray();
            n = pts.Length / 2;
            sy = new float[n];
            sorted = new float[n];
            v0 = Mathf.Floor(n * .72f);
            volume = v0;
            goal = n * .08f;

            // The angle where only 5% of the water could stay in: tilt past it and the starch slides out too.
            slide = 60;
            for (int deg = 10; deg <= 80; deg++)
            {
                float a = deg * Mathf.Deg2Rad;
                ComputeSy(a);
                Vector2 r = RimPoint(a);
                int keep = 0;
                for (int i = 0; i < n; i++)
                    if (sy[i] >= r.y)
                        keep++;
                if (keep <= n * .05f)
                {
                    slide = deg + 4;
                    break;
                }
            }

            rim = RimPoint(0);
        }

        private float Loss => 1 - starch / T0;
        private float Frac => Clamp((volume - goal) / (v0 - goal), 0, 1);
        private float Degrees => th * Mathf.Rad2Deg;
        private bool Held => (K.PointerDown && grab) || keyTilt;

        private static bool Inside(float x, float y) => y >= 0 && Sq(x / Rx) + Sq(y / Ry) <= 1;

        private static bool InStarch(float x, float y, float thickness)
        {
            if (y < Ry * .45f || thickness <= 0)
                return false;
            float bottom = Ry * Mathf.Sqrt(Mathf.Max(0, 1 - Sq(x / Rx)));
            return y > bottom - thickness;
        }

        private void ComputeSy(float a)
        {
            float s = Mathf.Sin(a), c = Mathf.Cos(a);
            for (int i = 0; i < n; i++)
                sy[i] = P.y + s * (pts[i * 2] - Pivot.x) + c * (pts[i * 2 + 1] - Pivot.y);
        }

        private static Vector2 RimPoint(float a)
        {
            float s = Mathf.Sin(a), c = Mathf.Cos(a), dx = Rx + 2 - Pivot.x, dy = -Pivot.y;
            return new Vector2(P.x + c * dx - s * dy, P.y + s * dx + c * dy);
        }

        public override void Update(float dt, bool active)
        {
            sound -= dt;
            warn = Mathf.Max(0, warn - dt);
            if (active)
            {
                t += dt;
                if (keyTilt)
                    keyX += dt * 38;
                float x = keyTilt ? keyX : Ptr.x;
                float target = Held ? Clamp((x - grabX) / 55, 0, 1) * MaxTilt : 0;
                float speed = 110 * Mathf.Deg2Rad * dt;
                th += Clamp(target - th, -speed, speed);
            }
            else
            {
                th = Mathf.Max(0, th - dt * 1.4f);
            }

            ComputeSy(th);
            Array.Copy(sy, sorted, n);
            Array.Sort(sorted);
            int v = Mathf.Max(0, Mathf.FloorToInt(volume));
            level = v > 0 ? sorted[n - v] : 1e9f;
            rim = RimPoint(th);
            if (active && v > 0)
            {
                int lo = 0, hi = n;
                while (lo < hi)
                {
                    int m = (lo + hi) >> 1;
                    if (sorted[m] < rim.y)
                        lo = m + 1;
                    else
                        hi = m;
                }

                int excess = Mathf.Max(0, lo - (n - v));
                if (excess > 0)
                {
                    float spill = Mathf.Min(excess, (excess * 5 + 50) * dt);
                    volume -= spill;
                    int drops = Mathf.Min(5, Mathf.CeilToInt(spill / 5));
                    for (int i = 0; i < drops; i++)
                        K.Parts.Emit(rim.x + R * 2, rim.y + R * 2, 16 + R * 26, 4 + R * 10, Hex(R < .5f ? "#a9cfc8" : "#d6ece6"), 1.6f, 280, 2, 125 + R * 3, true);
                    if (sound <= 0)
                    {
                        K.Play(Sfx.Pour);
                        sound = .1f;
                    }
                }
            }

            float deg = Degrees;
            if (active && deg > slide && starch > 0)
            {
                starch = Mathf.Max(0, starch - dt * (1 + (deg - slide) / 6) * 1.6f);
                warn = .6f;
                for (int i = 0; i < 2; i++)
                    K.Parts.Emit(rim.x, rim.y + 1, 14 + R * 16, 6, Hex(R < .5f ? "#f4f1e8" : "#e2ddd1"), 1.6f, 260, 2, 126, true);
            }

            if (active && !Done && volume <= goal && deg < 10 && !Held)
            {
                Done = true;
                Score = Mathf.RoundToInt(Clamp(100 - Loss * 110, 10, 100));
                Note = Loss < .05f ? "전분은 한 톨도 안 버렸어요" : Loss < .35f ? "전분이 조금 흘렀어요" : "전분을 많이 흘려버렸어요";
            }
        }

        public override void Down(Vector2 p)
        {
            grab = true;
            grabX = p.x - th / MaxTilt * 55;
        }

        public override void Up(Vector2 p) => grab = false;

        public override void Submit()
        {
            keyTilt = !keyTilt;
            if (keyTilt)
            {
                grabX = 0;
                keyX = th / MaxTilt * 55;
            }
        }

        public override (int, string) Timeout()
        {
            float left = Frac;
            return (Mathf.RoundToInt(Clamp(90 - Loss * 110 - left * 60, 15, 90)), left > .3f ? "윗물이 많이 남았어요" : "조금 늦었어요");
        }

        private static readonly Color32 Water = new Color32(146, 184, 176, 215), Surface = new Color32(205, 230, 222, 240),
            RimColor = new Color32(232, 246, 250, 240), Wall = new Color32(178, 214, 224, 165), Highlight = new Color32(246, 252, 255, 220),
            Foot = new Color32(160, 198, 210, 200), Edge = new Color32(70, 84, 96, 200), Empty = new Color32(210, 236, 242, 26);

        public override void Art(PixelCanvas a)
        {
            a.Blit(Pail, 234, 118);
            a.Ellipse(P.x, P.y + 23, 26, 4, Rgba(40, 20, 14, .35f));

            // Draw the glass bowl by rotating each screen pixel back into the bowl's own frame.
            float s = Mathf.Sin(th), c = Mathf.Cos(th);
            int ox = (int)P.x - 88, oy = (int)P.y - 82;
            for (int py = 0; py < RenderH; py++)
            {
                float screenY = oy + py + .5f, dy = screenY - P.y;
                for (int px = 0; px < RenderW; px++)
                {
                    float dx = ox + px + .5f - P.x;
                    float x = c * dx + s * dy + Pivot.x, y = -s * dx + c * dy + Pivot.y;
                    if (y < -3 || y > Ry + 7 || x < -Rx - 5 || x > Rx + 5)
                        continue;
                    int sx = ox + px, syi = oy + py;
                    float inner = Sq(x / Rx) + Sq(y / Ry);
                    if (y >= -1.6f && y <= .8f && Mathf.Abs(x) <= Rx + 3)
                    {
                        a.Blend(sx, syi, RimColor);
                        continue;
                    }

                    if (y > .8f && inner <= 1)
                    {
                        if (InStarch(x, y, starch))
                        {
                            int ix = Mathf.FloorToInt(x + 200), iy = Mathf.FloorToInt(y + 200);
                            a.Blend(sx, syi, Sh(KitchenArt.Starch, .35f + (Hash(ix, iy) - .5f) * .5f + (y > Ry - 3 ? -.15f : .15f), ix, iy));
                        }
                        else if (screenY >= level)
                        {
                            a.Blend(sx, syi, screenY < level + 1.3f ? Surface : Water);
                        }
                        else
                        {
                            a.Blend(sx, syi, Empty);
                        }

                        if (x > -Rx * .74f && x < -Rx * .64f && y > 3 && y < 14)
                            a.Blend(sx, syi, new Color32(246, 252, 255, 170));
                        continue;
                    }

                    float outer = Sq(x / (Rx + 3)) + Sq(y / (Ry + 3));
                    if (y > .8f && outer <= 1)
                    {
                        a.Blend(sx, syi, x < -Rx * .35f && y < Ry * .6f ? Highlight : Wall);
                        continue;
                    }

                    if (y > Ry + 1.5f && y <= Ry + 6 && Mathf.Abs(x) <= 15 - (y - Ry - 1.5f))
                    {
                        a.Blend(sx, syi, Foot);
                        continue;
                    }

                    if (y >= -2.6f && Sq(x / (Rx + 4.2f)) + Sq(y / (Ry + 4.2f)) <= 1)
                        a.Blend(sx, syi, Edge);
                }
            }
        }

        public override void ArtTop(PixelCanvas a)
        {
            if (!K.PointerDown && !keyTilt && Degrees < 3 && !Done)
                a.Blit(ArrowRight, 175 + Mathf.Sin(K.Time * 5) * 2, 82);
        }

        public override void Gauges(List<Gauge> list)
        {
            float f = Frac, kept = 1 - Loss, deg = Degrees;
            list.Add(new Gauge
            {
                Label = "남은 윗물", Value = f <= 0 ? "다 따라 냈어요!" : Mathf.RoundToInt(f * 100) + "%",
                Fill = f, FillColor = Hex("#7fb5d6"), ZoneMin = -1, Marker = -1
            });
            list.Add(new Gauge
            {
                Label = "바닥의 전분", Value = Mathf.RoundToInt(kept * 100) + "%",
                Fill = kept, FillColor = Hex(kept > .7f ? "#f1ede4" : "#e09a7a"), ZoneMin = -1, Marker = -1
            });
            list.Add(new Gauge
            {
                Label = "기울기", Value = Mathf.RoundToInt(deg) + "°", Fill = -1,
                ZoneMin = slide / 85, ZoneMax = 1, ZoneColor = Hex("#e07a6a", .45f), ZoneLabel = "전분이 쏟아짐",
                Marker = deg / 85, MarkerAlert = deg > slide
            });
        }

        public override Callout? Shout
        {
            get
            {
                if (warn > 0)
                    return new Callout("전분이 쏟아져요!", "#ff9d8a");
                if (volume <= goal && !Done)
                    return new Callout(keyTilt ? "확인 키로 그릇을 세워요" : "이제 손을 떼서 그릇을 세워요", "#a8dcb0", 30);
                return null;
            }
        }
    }

    // ====================================================================== 5. salt & mix
    internal sealed class MixGame : CookingGame
    {
        private const int Na = 96, Nr = 36, Bw = 124, Bh = 72;
        private const float Crx = 60, Cry = 34, ShakeTime = .55f;
        private static readonly Vector2 C = new Vector2(160, 103), Rest = new Vector2(268, 66);
        private static float[] tex;
        private readonly List<Vector2> salt = new List<Vector2>();
        private bool stirring;
        private int taps;
        private float anim, rot, acc, t, stirTime, mix, sound;
        private float? prevAngle;

        public MixGame(KitchenContext k) : base(k)
        {
            if (tex != null)
                return;
            tex = new float[Na * Nr];
            for (int r = 0; r < Nr; r++)
            for (int i = 0; i < Na; i++)
                tex[r * Na + i] = Clamp(.48f + (Vn(i * .38f, r * .95f) - .5f) * .75f + (Hash(i, r) - .5f) * .2f, 0, 1);
        }

        public override void Update(float dt, bool active)
        {
            sound -= dt;
            if (active)
            {
                t += dt;
                if (stirring)
                    stirTime += dt;
            }

            if (anim <= 0)
                return;
            anim = Mathf.Max(0, anim - dt);
            float k = 1 - anim / ShakeTime;
            if (k > .35f && k < .85f && R < .8f)
            {
                ShakerPose(out _, out _, out _, out float cx, out float cy);
                K.Parts.Emit(cx + (R - .5f) * 4, cy, (R - .5f) * 14, 20, new Color32(255, 255, 255, 255), 1, 160, 1, C.y - 10 + R * 20, true);
                if (R < .2f)
                    K.Play(Sfx.Salt);
            }

            if (anim == 0 && taps >= 2)
                stirring = true;
        }

        private void ShakerPose(out float x, out float y, out float r, out float capX, out float capY)
        {
            float k = anim > 0 ? 1 - anim / ShakeTime : 0;
            float e = Mathf.Sin(Mathf.Min(1, k) * Mathf.PI);
            x = Lerp(Rest.x, 212, e);
            y = Lerp(Rest.y, 46, e);
            r = Lerp(0, -2.3f, e) + (k > .3f && k < .85f ? Mathf.Sin(K.Time * 40) * .15f : 0);
            capX = x + 11 * Mathf.Sin(r);
            capY = y - 11 * Mathf.Cos(r);
        }

        public override void Down(Vector2 p)
        {
            if (Done)
                return;
            if (!stirring)
            {
                if (Mathf.Abs(p.x - Rest.x) < 16 && Mathf.Abs(p.y - Rest.y) < 20)
                    Shake();
            }
            else
            {
                prevAngle = null;
            }
        }

        private void Shake()
        {
            if (anim > 0 || taps >= 2)
                return;
            taps++;
            anim = ShakeTime;
            K.Play(Sfx.Salt);
            for (int i = 0; i < 14; i++)
                salt.Add(new Vector2(R * .8f, R * Mathf.PI * 2));
        }

        public override void Move(Vector2 p)
        {
            if (!stirring || Done)
                return;
            float nx = (p.x - C.x) / Crx, ny = (p.y - C.y) / Cry, r = Mathf.Sqrt(nx * nx + ny * ny), ang = Mathf.Atan2(ny, nx);
            if (r > .12f && r < 1.35f)
            {
                if (prevAngle.HasValue)
                {
                    float d = ang - prevAngle.Value;
                    d = ((d + Mathf.PI) % (Mathf.PI * 2) + Mathf.PI * 2) % (Mathf.PI * 2) - Mathf.PI;
                    if (Mathf.Abs(d) < 1.2f)
                        Stir(d);
                }

                prevAngle = ang;
            }
            else
            {
                prevAngle = null;
            }
        }

        private void Stir(float d)
        {
            acc += Mathf.Abs(d);
            rot += d;
            if (sound <= 0)
            {
                K.Play(Sfx.Stir);
                sound = .16f;
            }

            mix = Mathf.Min(1, acc / (Mathf.PI * 6));
            if (mix < 1 || Done)
                return;
            Done = true;
            Score = TimeScore(stirTime, 6, 13);
            Note = stirTime <= 6 ? "반죽이 골고루 섞였어요" : "잘 섞였어요. 조금 더 빠르게 저어 봐요";
        }

        public override void Up(Vector2 p) => prevAngle = null;

        public override void Submit()
        {
            if (Done)
                return;
            if (!stirring)
                Shake();
            else
                Stir(Mathf.PI / 2);
        }

        public override (int, string) Timeout() =>
            !stirring ? (20, "소금을 다 넣지 못했어요") : (Mathf.RoundToInt(30 + mix * 40), "덜 섞였어요");

        public override void Art(PixelCanvas a)
        {
            a.Ellipse(160, 152, 70, 7, Rgba(40, 20, 14, .35f));
            a.Blit(MixBowl, C.x - 82, C.y - 55);

            // Batter: a polar texture twisted by the stirring, with a pale patch of starch that fades as it mixes in.
            const float blobR = .42f, blobA = .7f;
            float bk = .35f + .65f * (1 - blobR), ba = blobA + rot * bk;
            float bx = Mathf.Cos(ba) * blobR, by = Mathf.Sin(ba) * blobR, br = .3f * Mathf.Pow(1 - mix, .8f);
            int ox = (int)C.x - 62, oy = (int)C.y - 36;
            for (int y = 0; y < Bh; y++)
            for (int x = 0; x < Bw; x++)
            {
                float nx = (x + .5f - 62) / Crx, ny = (y + .5f - 36) / Cry, r = Mathf.Sqrt(nx * nx + ny * ny);
                if (r > 1)
                    continue;
                float ang = Mathf.Atan2(ny, nx), k = .35f + .65f * (1 - r), a2 = ang - rot * k;
                int ai = ((int)Math.Floor(a2 / (Mathf.PI * 2) * Na) % Na + Na) % Na, ri = Mathf.Min(Nr - 1, (int)(r * Nr));
                float l = tex[ri * Na + ai] * .85f + .1f - nx * .08f - ny * .12f + mix * .08f;
                if (r > .88f)
                    l -= (r - .88f) * 2.5f;
                float bd = Mathf.Sqrt(Sq(nx - bx) + Sq(ny - by));
                Color32 col = br > .02f && bd < br ? Sh(KitchenArt.Starch, .45f + (1 - bd / br) * .45f - ny * .1f, x, y) : Sh(Flesh, l, x, y);
                a.Blend(ox + x, oy + y, col);
            }

            float saltAlpha = Clamp(1 - mix * 1.6f, 0, 1);
            if (saltAlpha > 0)
                foreach (Vector2 s in salt)
                {
                    float angle = s.y + rot * (.35f + .65f * (1 - s.x));
                    a.FillRect(Mathf.Round(C.x + Mathf.Cos(angle) * s.x * Crx), Mathf.Round(C.y + Mathf.Sin(angle) * s.x * Cry), 1, 1,
                        new Color32(255, 255, 255, 255), saltAlpha);
                }

            if (!stirring || Done)
                return;
            for (int i = 0; i < 16; i++)
            {
                float angle = i / 16f * Mathf.PI * 2 + K.Time * 2.4f;
                bool on = (i + Mathf.FloorToInt(K.Time * 8)) % 4 == 0;
                a.FillRect(Mathf.Round(C.x + Mathf.Cos(angle) * Crx * .72f), Mathf.Round(C.y + Mathf.Sin(angle) * Cry * .72f), 2, 2,
                    Hex("#FFC05A", on ? 1 : .35f));
            }
        }

        public override void ArtTop(PixelCanvas a)
        {
            ShakerPose(out float x, out float y, out float r, out _, out _);
            a.BlitRotated(Shaker, Mathf.Round(x), Mathf.Round(y), r, -10, -15);
            if (!stirring && anim == 0 && !Done)
                a.Blit(ArrowDown, Rest.x - 3, Rest.y - 30 + Bob(6, 2));
            if (stirring && K.ShowTool)
                a.Blit(Spoon, Mathf.Round(Ptr.x - 7), Mathf.Round(Ptr.y - 25));
        }

        public override void Gauges(List<Gauge> list) => list.Add(!stirring
            ? Gauge.Meter("소금 톡톡", taps / 2f, $"{taps} / 2", Hex("#e9e4d8"))
            : Gauge.Meter("섞은 바퀴", mix, $"{acc / (Mathf.PI * 2):0.0} / 3"));
    }

    // ====================================================================== shared pan
    internal static class Stove
    {
        public static readonly Vector2 Center = new Vector2(146, 100);
        public const int Mw = 140, Mh = 96;
        private static float[] batter;

        public static float[] Batter
        {
            get
            {
                if (batter != null)
                    return batter;
                batter = new float[Mw * Mh];
                for (int y = 0; y < Mh; y++)
                for (int x = 0; x < Mw; x++)
                    batter[y * Mw + x] = Clamp(.5f + (Vn(x / 2.4f, y / 1.4f) - .5f) * .85f + (Hash(x, y) - .5f) * .25f, 0, 1);
                return batter;
            }
        }

        public static bool InPan(int x, int y, float rx, float ry) => Sq((x + .5f - 70) / rx) + Sq((y + .5f - 48) / ry) <= 1;

        public static byte[] DefaultMask()
        {
            var mask = new byte[Mw * Mh];
            for (int y = 0; y < Mh; y++)
            for (int x = 0; x < Mw; x++)
                if (InPan(x, y, 58, 39))
                    mask[y * Mw + x] = 1;
            return mask;
        }

        public static void Draw(PixelCanvas a, float time)
        {
            a.Ellipse(Center.x, Center.y + 6, 112, 76, Rgba(255, 140, 50, .07f));
            a.Ellipse(Center.x, Center.y + 6, 98, 66, Rgba(255, 150, 60, .08f));
            a.Ellipse(Center.x + 4, Center.y + 16, 84, 54, Rgba(10, 6, 6, .45f));
            const int flames = 18;
            for (int i = 0; i < flames; i++)
            {
                float angle = Mathf.PI * (.08f + .84f * i / (flames - 1));
                float ex = Center.x + Mathf.Cos(angle) * 82, ey = Center.y + 2 + Mathf.Sin(angle) * 57;
                int h = 2 + (Mathf.Sin(time * 22 + i * 1.7f) > 0 ? 1 : 0) + (int)(Hash(i, Mathf.FloorToInt(time * 12)) * 2);
                for (int k = 0; k < h; k++)
                    a.FillRect(Mathf.Round(ex + Mathf.Cos(angle) * k), Mathf.Round(ey + Mathf.Sin(angle) * k), 2, 1,
                        Hex(k == 0 ? "#6ab8ff" : k < h - 1 ? "#ffd166" : "#ff7b2e"));
            }

            a.Blit(Handle, Center.x + 72, Center.y - 12);
            a.Blit(Pan, Center.x - 86, Center.y - 62);
        }
    }

    // ====================================================================== 6. oil & spread
    internal sealed class SpreadGame : CookingGame
    {
        private enum Phase { Oil, Drop, Spread }
        private const int Mw = Stove.Mw, Mh = Stove.Mh;
        private static readonly Vector2 Pc = Stove.Center;
        private readonly byte[] mask = new byte[Mw * Mh];
        private readonly int target;
        private readonly PixelSprite batter = new PixelSprite(Mw, Mh);
        private Phase phase = Phase.Oil;
        private float oil, tilt, t, spreadTime, sound, wait, keyAngle;
        private bool pouring, dirty = true;
        private int covered, oilScore;
        private string oilNote = "";
        private Vector2? prev;

        public SpreadGame(KitchenContext k) : base(k)
        {
            for (int y = 0; y < Mh; y++)
            for (int x = 0; x < Mw; x++)
                if (Stove.InPan(x, y, 60, 40))
                    target++;
        }

        private float Coverage => (float)covered / target;

        public override void Update(float dt, bool active)
        {
            sound -= dt;
            tilt = Clamp(tilt + (pouring ? dt * 3.5f : -dt * 3), 0, 1);
            if (!active)
                return;
            t += dt;
            if (phase == Phase.Oil && pouring && tilt > .85f)
            {
                oil = Mathf.Min(1.05f, oil + dt * .36f);
                Mouth(out float mx, out float my, out _, out _, out _);
                if (R < .9f)
                    K.Parts.Emit(mx + (R - .5f), my, 0, 30, Hex(R < .5f ? "#ecc95a" : "#d4a632"), .8f, 240, 1, Pc.y - 4 + R * 8, true);
                if (sound <= 0)
                {
                    K.Play(Sfx.Pour);
                    sound = .12f;
                }

                if (oil >= 1.05f)
                    EndOil();
            }

            if (phase == Phase.Drop)
            {
                wait -= dt;
                if (wait <= 0)
                {
                    phase = Phase.Spread;
                    Paint(70, 48, true);
                    K.Play(Sfx.Plop);
                    for (int i = 0; i < 14; i++)
                        K.Parts.Emit(Pc.x + (R - .5f) * 20, Pc.y + (R - .5f) * 10, (R - .5f) * 40, -30, Raw[3], .6f, 200, 2);
                }
            }

            if (phase != Phase.Spread)
                return;
            spreadTime += dt;
            if (R < .5f)
            {
                int i = (int)(R * Mw * Mh);
                if (mask[i] != 0)
                    K.Parts.Emit(Pc.x - 70 + i % Mw, Pc.y - 48 + i / Mw, (R - .5f) * 6, -10, Hex("#fff4d0"), .25f, 0);
            }
        }

        private void Mouth(out float x, out float y, out float bx, out float by, out float r)
        {
            float e = tilt;
            bx = Lerp(36, 104, e);
            by = Lerp(72, 40, e);
            r = Lerp(0, 2.1f, e);
            x = bx + Mathf.Sin(r) * 20;
            y = by - Mathf.Cos(r) * 20;
        }

        private void EndOil()
        {
            pouring = false;
            float o = oil;
            if (o >= .5f && o <= .8f)
            {
                oilScore = Mathf.RoundToInt(100 - Mathf.Abs(o - .65f) * 60);
                oilNote = "기름 양이 딱 좋아요";
            }
            else if (o < .5f)
            {
                oilScore = Mathf.RoundToInt(55 + (o - .12f) / .38f * 30);
                oilNote = "기름이 조금 적어요";
            }
            else
            {
                oilScore = Mathf.RoundToInt(Clamp(80 - (o - .8f) * 200, 40, 80));
                oilNote = "기름이 너무 많아요";
            }

            phase = Phase.Drop;
            wait = .5f;
        }

        private int Paint(float cx, float cy, bool first)
        {
            int painted = 0;
            float rx = first ? 16 : 8.5f, ry = first ? 11 : 6.5f;
            for (int y = Mathf.FloorToInt(cy - ry); y <= cy + ry; y++)
            for (int x = Mathf.FloorToInt(cx - rx); x <= cx + rx; x++)
            {
                if (x < 0 || y < 0 || x >= Mw || y >= Mh)
                    continue;
                if (Sq((x + .5f - cx) / rx) + Sq((y + .5f - cy) / ry) > 1 || !Stove.InPan(x, y, 66, 44))
                    continue;
                int i = y * Mw + x;
                if (mask[i] != 0)
                    continue;
                mask[i] = 1;
                if (Stove.InPan(x, y, 60, 40))
                    covered++;
                painted++;
            }

            if (painted > 0)
                dirty = true;
            return painted;
        }

        public override void Down(Vector2 p)
        {
            if (Done)
                return;
            if (phase == Phase.Oil)
                pouring = true;
            else
                prev = p;
        }

        public override void Move(Vector2 p)
        {
            if (phase != Phase.Spread || Done || prev == null)
                return;
            Vector2 from = prev.Value;
            float dx = p.x - from.x, dy = p.y - from.y;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(dx * dx + dy * dy) / 2));
            int painted = 0;
            for (int s = 1; s <= steps; s++)
                painted += Paint(from.x + dx * s / steps - Pc.x + 70, from.y + dy * s / steps - Pc.y + 48, false);
            prev = p;
            if (painted > 0 && sound <= 0)
            {
                K.Play(Sfx.Sizzle);
                sound = .14f;
            }

            CheckDone();
        }

        private void CheckDone()
        {
            if (Done || Coverage < .86f)
                return;
            Done = true;
            int spreadScore = TimeScore(spreadTime, 6.5f, 14);
            Score = Mathf.RoundToInt(oilScore * .35f + spreadScore * .65f);
            Note = oilScore < 70 ? oilNote : spreadScore >= 90 ? "가장자리까지 얇게 잘 펴졌어요" : "잘 폈어요. 조금 더 빠르게!";
            K.PancakeMask = (byte[])mask.Clone();
        }

        public override void Up(Vector2 p)
        {
            if (phase == Phase.Oil && pouring)
            {
                if (oil >= .12f)
                    EndOil();
                else
                    pouring = false;
            }

            prev = null;
        }

        public override void Submit()
        {
            if (Done)
                return;
            if (phase == Phase.Oil)
            {
                if (!pouring)
                    pouring = true;
                else
                    Up(Vector2.zero);
                return;
            }

            if (phase != Phase.Spread)
                return;
            // Three spokes from the middle out to the rim per press.
            for (int spoke = 0; spoke < 3; spoke++, keyAngle += .36f)
                for (float r = 0; r <= 1.05f; r += .06f)
                    Paint(70 + Mathf.Cos(keyAngle) * 60 * r, 48 + Mathf.Sin(keyAngle) * 40 * r, false);
            K.Play(Sfx.Sizzle);
            CheckDone();
        }

        public override (int, string) Timeout()
        {
            K.PancakeMask = (byte[])mask.Clone();
            if (phase == Phase.Oil)
            {
                if (oil < .12f)
                    K.PancakeMask = null;
                return (20, "기름을 두르지 못했어요");
            }

            return (Mathf.RoundToInt(oilScore * .35f + Coverage * 60 * .65f), "반죽이 다 펴지지 않았어요");
        }

        private void Render()
        {
            float[] tex = Stove.Batter;
            for (int y = 0; y < Mh; y++)
            for (int x = 0; x < Mw; x++)
            {
                int i = y * Mw + x;
                if (mask[i] == 0)
                {
                    batter.Px[i] = None;
                    continue;
                }

                bool edge = (x > 0 && mask[i - 1] == 0) || (x < Mw - 1 && mask[i + 1] == 0) || (y > 0 && mask[i - Mw] == 0) || (y < Mh - 1 && mask[i + Mw] == 0);
                batter.Px[i] = edge ? Sh(KitchenArt.Cook, .3f, x, y) : Sh(Raw, .3f + tex[i] * .6f - (y - 48) / 48f * .08f, x, y);
            }

            dirty = false;
        }

        public override void Art(PixelCanvas a)
        {
            Stove.Draw(a, K.Time);
            float o = Clamp(oil, 0, 1.05f);
            if (o > 0)
            {
                float so = Mathf.Sqrt(o);
                a.Ellipse(Pc.x, Pc.y, 62 * so, 42 * so, Rgba(214, 170, 60, .22f));
                Color32 shine = Rgba(255, 240, 180, .5f);
                a.FillRect(Mathf.Round(Pc.x - 40 * so), Mathf.Round(Pc.y - 26 * so), Mathf.Round(16 * so) + 1, 1, shine);
                a.FillRect(Mathf.Round(Pc.x - 44 * so), Mathf.Round(Pc.y - 22 * so), 2, Mathf.Round(8 * so) + 1, shine);
            }

            if (dirty)
                Render();
            a.Blit(batter, Pc.x - 70, Pc.y - 48);
            if (phase != Phase.Spread || Done)
                return;
            for (int i = 0; i < 24; i++)
            {
                float angle = i / 24f * Mathf.PI * 2;
                int x = Mathf.RoundToInt(70 + Mathf.Cos(angle) * 60), y = Mathf.RoundToInt(48 + Mathf.Sin(angle) * 40);
                if (mask[y * Mw + x] == 0 && (i + Mathf.FloorToInt(K.Time * 6)) % 3 == 0)
                    a.FillRect(Pc.x - 70 + x, Pc.y - 48 + y, 2, 2, Hex("#FFC05A"));
            }
        }

        public override void ArtTop(PixelCanvas a)
        {
            Mouth(out _, out _, out float bx, out float by, out float r);
            if (phase == Phase.Oil || tilt > 0)
                a.BlitRotated(Bottle, Mathf.Round(bx), Mathf.Round(by), r, -9, -22);
            if (phase == Phase.Oil && !pouring && !Done)
                a.Blit(ArrowDown, 33, 36 + Bob(6, 2));
            if (phase == Phase.Spread && K.ShowTool)
                a.Blit(Spoon, Mathf.Round(Ptr.x - 7), Mathf.Round(Ptr.y - 25));
        }

        public override void Gauges(List<Gauge> list)
        {
            if (phase == Phase.Spread)
            {
                list.Add(Gauge.Meter("펴진 반죽", Coverage / .86f));
                return;
            }

            list.Add(new Gauge
            {
                Label = "두른 기름", Value = Mathf.RoundToInt(Clamp(oil, 0, 1) * 100) + "%",
                Fill = Clamp(oil, 0, 1), FillColor = Hex(oil > .8f ? "#d86a5a" : oil >= .5f ? "#8fd0a0" : "#e6c25a"),
                ZoneMin = .5f, ZoneMax = .8f, ZoneColor = Hex("#689c7c", .55f), ZoneLabel = "딱 좋아요", Marker = -1
            });
        }

        public override Callout? Shout => pouring && oil >= .5f && oil <= .8f
            ? new Callout("지금 떼요!", "#a8dcb0")
            : (Callout?)null;
    }

    // ====================================================================== 7. fry & flip
    internal sealed class FryGame : CookingGame
    {
        private enum Phase { A, Flip, B, Serve, Finished }
        private const int Mw = Stove.Mw, Mh = Stove.Mh;
        private static readonly Vector2 Pc = Stove.Center;
        private readonly byte[] mask;
        private readonly PixelSprite cake = new PixelSprite(Mw, Mh);
        private readonly List<int> cells = new List<int>(), edges = new List<int>();
        private sbyte[] dist;
        private Phase phase = Phase.A;
        private float d1, d2, d1Final, d2Final, flipTime, t, sound, swipeY, swipeT;
        private int s1, s2;
        private bool swiping;

        public FryGame(KitchenContext k) : base(k)
        {
            mask = k.PancakeMask != null ? (byte[])k.PancakeMask.Clone() : Stove.DefaultMask();
            int count = 0;
            foreach (byte v in mask)
                count += v;
            if (count < 200)
                mask = Stove.DefaultMask();
            Build();
        }

        private void Build()
        {
            cells.Clear();
            for (int i = 0; i < mask.Length; i++)
                if (mask[i] != 0)
                    cells.Add(i);
            dist = new sbyte[Mw * Mh];
            foreach (int i in cells)
                dist[i] = 99;
            // Rings counted in from the edge: 1 is the outermost pixel, up to 4.
            for (sbyte ring = 1; ring <= 4; ring++)
            {
                var next = new List<int>();
                foreach (int i in cells)
                {
                    if (dist[i] != 99)
                        continue;
                    int x = i % Mw, y = i / Mw, want = ring - 1;
                    if ((x > 0 ? dist[i - 1] : 0) == want || (x < Mw - 1 ? dist[i + 1] : 0) == want ||
                        (y > 0 ? dist[i - Mw] : 0) == want || (y < Mh - 1 ? dist[i + Mw] : 0) == want)
                        next.Add(i);
                }

                foreach (int i in next)
                    dist[i] = ring;
            }

            edges.Clear();
            foreach (int i in cells)
                if (dist[i] == 1)
                    edges.Add(i);
        }

        private void Tear()
        {
            for (int x = 0; x < Mw; x++)
            {
                int y = Mathf.RoundToInt(48 + Mathf.Sin(x * .35f) * 3 + (x - 70) * .12f);
                for (int k = 0; k < 2; k++)
                    if (y + k >= 0 && y + k < Mh)
                        mask[(y + k) * Mw + x] = 0;
            }

            Build();
        }

        private static int Timing(float d)
        {
            if (d < .4f) return 25;
            if (d < .62f) return Mathf.RoundToInt(55 + (d - .4f) / .22f * 33);
            if (d <= .8f) return Mathf.RoundToInt(100 - Mathf.Abs(d - .71f) * 70);
            if (d <= .95f) return Mathf.RoundToInt(85 - (d - .8f) / .15f * 45);
            return 15;
        }

        private static string TimingNote(float d)
        {
            if (d < .4f) return "너무 일찍 뒤집어 찢어졌어요";
            if (d < .62f) return "조금 덜 익었어요";
            if (d <= .8f) return "노릇노릇 완벽해요";
            if (d <= .95f) return "살짝 진하게 익었어요";
            return "타 버렸어요";
        }

        private float Current => phase == Phase.A ? d1 : phase == Phase.B ? d2 : phase == Phase.Flip ? d1Final : d2Final;
        private bool Cooking => phase == Phase.A || phase == Phase.B;

        public override void Update(float dt, bool active)
        {
            sound -= dt;
            if (phase == Phase.Flip)
            {
                flipTime += dt / .8f;
                if (flipTime >= 1)
                {
                    phase = Phase.B;
                    flipTime = 0;
                    K.Shake = .5f;
                    for (int i = 0; i < 24; i++)
                        K.Parts.Emit(Pc.x + (R - .5f) * 110, Pc.y + (R - .5f) * 70, (R - .5f) * 30, -20, Hex("#fff4d0"), .35f, 0);
                    K.Play(Sfx.Sizzle);
                }
            }

            if (phase == Phase.Serve)
            {
                flipTime += dt / .7f;
                if (flipTime >= 1)
                {
                    phase = Phase.Finished;
                    Done = true;
                    Score = Mathf.RoundToInt((s1 + s2) / 2f);
                    Note = TimingNote(s1 <= s2 ? d1Final : d2Final);
                }
            }

            if (!active)
                return;
            t += dt;
            if (phase == Phase.A)
            {
                d1 += dt / 5.2f;
                if (d1 > 1.25f)
                    Flip();
            }
            else if (phase == Phase.B)
            {
                d2 += dt / 4.6f;
                if (d2 > 1.25f)
                    Serve();
            }

            if (!Cooking || edges.Count == 0)
                return;
            for (int k = 0; k < 3; k++)
            {
                int i = edges[(int)(R * edges.Count) % edges.Count];
                K.Parts.Emit(Pc.x - 70 + i % Mw + (R - .5f) * 3, Pc.y - 48 + i / Mw, (R - .5f) * 8, -8, Hex("#fff4d0"), .22f, 0);
            }

            if (Current > .92f && R < .5f)
                K.Parts.Emit(Pc.x + (R - .5f) * 80, Pc.y + (R - .5f) * 40, (R - .5f) * 6, -14, Rgba(120, 108, 104, .7f), 1.4f, -6, 2, float.NaN, false, true);
            if (sound <= 0)
            {
                K.Play(Sfx.Sizzle);
                sound = .2f;
            }
        }

        private void Flip()
        {
            d1Final = d1;
            s1 = Timing(d1);
            if (d1 < .4f)
                Tear();
            phase = Phase.Flip;
            flipTime = 0;
            K.Play(Sfx.Flip);
        }

        private void Serve()
        {
            d2Final = d2;
            s2 = Timing(d2);
            phase = Phase.Serve;
            flipTime = 0;
            K.Play(Sfx.Flip);
            Render(true);
            K.FinalPancake = cake.Copy();
        }

        public override void Down(Vector2 p)
        {
            if (!Cooking)
                return;
            swiping = true;
            swipeY = p.y;
            swipeT = t;
        }

        public override void Move(Vector2 p)
        {
            if (!swiping || swipeY - p.y <= 12 || t - swipeT >= .7f)
                return;
            swiping = false;
            Submit();
        }

        public override void Up(Vector2 p) => swiping = false;

        public override void Submit()
        {
            if (phase == Phase.A)
                Flip();
            else if (phase == Phase.B)
                Serve();
        }

        public override (int, string) Timeout()
        {
            if (phase == Phase.A)
            {
                d1Final = d1;
                s1 = Timing(d1);
            }

            int second = phase == Phase.B ? Timing(d2) : s2;
            if (K.FinalPancake == null)
            {
                Render(true);
                K.FinalPancake = cake.Copy();
            }

            return (Mathf.RoundToInt((s1 + second) / 2f), "제때 뒤집지 못했어요");
        }

        private void Render(bool final)
        {
            float[] tex = Stove.Batter;
            bool flipped = final || phase == Phase.B || phase == Phase.Serve || phase == Phase.Finished || (phase == Phase.Flip && flipTime >= .5f);
            Array.Clear(cake.Px, 0, cake.Px.Length);
            foreach (int i in cells)
            {
                int x = i % Mw, y = i / Mw, e = dist[i];
                float l = tex[i];
                Color32 col;
                if (!flipped)
                {
                    col = Sh(Raw, .32f + l * .55f - d1 * .06f, x, y);
                    if (e <= 2)
                        col = Mix(col, CookColor(d1), (3 - e) / 3f * Clamp(d1 * 1.6f, 0, 1));
                    if (Hash(x, y) < .035f * (1 - Clamp(d1, 0, 1)))
                        col = new Color32(255, 250, 236, 255);
                }
                else
                {
                    Color32 baseColor = CookColor(d1Final + (l < .28f ? .14f : 0) - (l > .8f ? .05f : 0));
                    col = Px.Scale(baseColor, .9f + l * .18f);
                    if (!final && e <= 2)
                        col = Mix(col, CookColor(d2 * .9f + .1f), (3 - e) / 3f * Clamp(d2 * 1.5f, 0, 1));
                    if (e == 1)
                        col = Px.Scale(col, .85f);
                }

                cake.Px[i] = col;
            }
        }

        public override void Art(PixelCanvas a)
        {
            Stove.Draw(a, K.Time);
            if (phase == Phase.Finished)
                return;
            Render(false);
            float lift = 0, squashY = 1;
            if (phase == Phase.Flip)
            {
                lift = Mathf.Sin(Mathf.PI * flipTime) * 36;
                squashY = Mathf.Max(.08f, Mathf.Abs(Mathf.Cos(Mathf.PI * flipTime)));
            }

            if (phase == Phase.Serve)
                lift = flipTime * flipTime * 150;
            if (lift > 2)
                a.Ellipse(Pc.x, Pc.y + 2, 60 * (1 - lift / 220), 40 * (1 - lift / 220), Rgba(0, 0, 0, .35f));
            int h = Mathf.Max(2, Mathf.RoundToInt(Mh * squashY));
            a.BlitScaled(cake, Pc.x - 70, Mathf.Round(Pc.y - lift - h / 2f), Mw, h);
        }

        public override void ArtTop(PixelCanvas a)
        {
            if (Cooking && K.ShowTool)
                a.Blit(Turner, Mathf.Round(Ptr.x - 8), Mathf.Round(Ptr.y - 26));
        }

        public override void Gauges(List<Gauge> list) => list.Add(new Gauge
        {
            Label = phase == Phase.A || phase == Phase.Flip ? "첫 번째 면 · 아랫면" : "두 번째 면 · 아랫면",
            Value = "",
            Fill = -1,
            CookGradient = true,
            ZoneMin = .62f / 1.2f,
            ZoneMax = .8f / 1.2f,
            ZoneColor = Hex("#FFC05A", .25f),
            ZoneLabel = "노릇노릇",
            Marker = Clamp(Current / 1.2f, 0, 1)
        });

        public override Callout? Shout
        {
            get
            {
                if (!Cooking)
                    return null;
                float cur = Current;
                if (cur > .92f)
                    return new Callout("타고 있어요!", "#ff9d8a", 38);
                if (cur >= .62f && cur <= .8f)
                    return new Callout(phase == Phase.A ? "지금! 위로 휙 뒤집어요" : "지금! 위로 휙 담아요", "#FFC05A", 38);
                return null;
            }
        }
    }
}
