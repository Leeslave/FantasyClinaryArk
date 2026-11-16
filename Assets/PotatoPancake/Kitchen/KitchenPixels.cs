using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyClinaryArk
{
    /// <summary>A small RGBA image addressed with y pointing down, like the art it was drawn from.</summary>
    internal sealed class PixelSprite
    {
        public readonly int W;
        public readonly int H;
        public readonly Color32[] Px;

        public PixelSprite(int w, int h)
        {
            W = w;
            H = h;
            Px = new Color32[w * h];
        }

        public PixelSprite Copy()
        {
            var copy = new PixelSprite(W, H);
            Array.Copy(Px, copy.Px, Px.Length);
            return copy;
        }

        public Texture2D ToTexture()
        {
            var texture = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var rows = new Color32[Px.Length];
            for (int y = 0; y < H; y++)
                Array.Copy(Px, y * W, rows, (H - 1 - y) * W, W);
            texture.SetPixels32(rows);
            texture.Apply(false);
            return texture;
        }
    }

    /// <summary>Colour, noise and shading helpers shared by every procedural sprite.</summary>
    internal static class Px
    {
        public static readonly Color32 None = new Color32(0, 0, 0, 0);

        private static readonly float[] Bayer =
            { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        static Px()
        {
            for (int i = 0; i < Bayer.Length; i++)
                Bayer[i] = (Bayer[i] + .5f) / 16f;
        }

        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
        public static float Lerp(float p, float q, float t) => p + (q - p) * t;
        public static float Sq(float v) => v * v;
        public static float Rnd() => UnityEngine.Random.value;

        public static Color32 Hex(string hex, float alpha = 1)
        {
            int Part(int at) => Convert.ToInt32(hex.Substring(at, 2), 16);
            return new Color32((byte)Part(1), (byte)Part(3), (byte)Part(5), (byte)Mathf.RoundToInt(alpha * 255));
        }

        public static Color32 Rgba(int r, int g, int b, float alpha) =>
            new Color32((byte)r, (byte)g, (byte)b, (byte)Mathf.RoundToInt(alpha * 255));

        public static Color32[] Ramp(params string[] hexes)
        {
            var ramp = new Color32[hexes.Length];
            for (int i = 0; i < hexes.Length; i++)
                ramp[i] = Hex(hexes[i]);
            return ramp;
        }

        public static Color32[] Tail(Color32[] ramp)
        {
            var tail = new Color32[ramp.Length - 1];
            Array.Copy(ramp, 1, tail, 0, tail.Length);
            return tail;
        }

        /// <summary>Picks a ramp colour for a light level, ordered-dithering between neighbours.</summary>
        public static Color32 Sh(Color32[] ramp, float light, int x, int y)
        {
            light = Clamp(light, 0, .999f);
            float v = light * (ramp.Length - 1);
            int b = (int)Math.Floor(v);
            int index = b + (v - b > Bayer[((y & 3) << 2) | (x & 3)] ? 1 : 0);
            return ramp[Math.Min(ramp.Length - 1, index)];
        }

        public static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (int)((uint)h ^ ((uint)h >> 13)) * 1274126177;
                uint u = (uint)h;
                return (u ^ (u >> 16)) / 4294967295f;
            }
        }

        public static float Vn(float x, float y)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
            float p = Hash(xi, yi), q = Hash(xi + 1, yi), r = Hash(xi, yi + 1), s = Hash(xi + 1, yi + 1);
            return p + (q - p) * u + (r - p) * v + (p - q - r + s) * u * v;
        }

        public static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay;
            float t = Clamp(((px - ax) * vx + (py - ay) * vy) / (vx * vx + vy * vy), 0, 1);
            return Mathf.Sqrt(Sq(px - ax - vx * t) + Sq(py - ay - vy * t));
        }

        public static Color32 Mix(Color32 p, Color32 q, float t) => new Color32(
            (byte)Lerp(p.r, q.r, t), (byte)Lerp(p.g, q.g, t), (byte)Lerp(p.b, q.b, t), 255);

        public static Color32 Scale(Color32 p, float k) => new Color32(
            (byte)Clamp(p.r * k, 0, 255), (byte)Clamp(p.g * k, 0, 255), (byte)Clamp(p.b * k, 0, 255), 255);

        public static Color32 WithAlpha(Color32 c, float alpha) =>
            new Color32(c.r, c.g, c.b, (byte)Mathf.RoundToInt(Clamp(alpha, 0, 1) * 255));

        public static float EaseOutBack(float t)
        {
            const float c = 1.7f;
            return 1 + (c + 1) * Mathf.Pow(t - 1, 3) + c * Mathf.Pow(t - 1, 2);
        }

        public static PixelSprite Make(int w, int h, Func<int, int, Color32> paint, Color32? outline = null)
        {
            var sprite = new PixelSprite(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c = paint(x, y);
                if (c.a > 0)
                    sprite.Px[y * w + x] = c;
            }

            if (outline.HasValue)
                Outline(sprite, outline.Value);
            return sprite;
        }

        public static void Outline(PixelSprite s, Color32 ink)
        {
            int w = s.W, h = s.H;
            var solid = new bool[w * h];
            for (int i = 0; i < solid.Length; i++)
                solid[i] = s.Px[i].a > 100;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (solid[i])
                    continue;
                if ((x > 0 && solid[i - 1]) || (x < w - 1 && solid[i + 1]) ||
                    (y > 0 && solid[i - w]) || (y < h - 1 && solid[i + w]))
                    s.Px[i] = new Color32(ink.r, ink.g, ink.b, 255);
            }
        }

        public static PixelSprite FromMap(string[] map, Dictionary<char, Color32> palette)
        {
            int h = map.Length, w = map[0].Length;
            return Make(w, h, (x, y) => palette.TryGetValue(map[y][x], out Color32 c) ? c : None);
        }

        public static PixelSprite FlipY(PixelSprite s)
        {
            var flipped = new PixelSprite(s.W, s.H);
            for (int y = 0; y < s.H; y++)
                Array.Copy(s.Px, y * s.W, flipped.Px, (s.H - 1 - y) * s.W, s.W);
            return flipped;
        }
    }

    /// <summary>The 320 × 180 art layer: everything in the kitchen is painted here and shown 4× with point filtering.</summary>
    internal sealed class PixelCanvas
    {
        public readonly int W;
        public readonly int H;
        public readonly Color32[] Buf;
        public readonly Texture2D Texture;
        private readonly Color32[] upload;

        public PixelCanvas(int w, int h)
        {
            W = w;
            H = h;
            Buf = new Color32[w * h];
            upload = new Color32[w * h];
            Texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Kitchen art"
            };
        }

        public void Clear(Color32 color)
        {
            for (int i = 0; i < Buf.Length; i++)
                Buf[i] = color;
        }

        public void Upload()
        {
            for (int y = 0; y < H; y++)
                Array.Copy(Buf, y * W, upload, (H - 1 - y) * W, W);
            Texture.SetPixels32(upload);
            Texture.Apply(false);
        }

        public void Blend(int x, int y, Color32 c, float alpha = 1)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H)
                return;
            float sa = c.a / 255f * alpha;
            if (sa <= 0)
                return;
            int i = y * W + x;
            if (sa >= .999f)
            {
                Buf[i] = new Color32(c.r, c.g, c.b, 255);
                return;
            }

            Color32 d = Buf[i];
            float da = d.a / 255f;
            float keep = da * (1 - sa);
            float oa = sa + keep;
            Buf[i] = new Color32(
                (byte)((c.r * sa + d.r * keep) / oa),
                (byte)((c.g * sa + d.g * keep) / oa),
                (byte)((c.b * sa + d.b * keep) / oa),
                (byte)(oa * 255));
        }

        public void Blit(PixelSprite s, float fx, float fy, float alpha = 1)
        {
            int x0 = Mathf.RoundToInt(fx), y0 = Mathf.RoundToInt(fy);
            for (int y = 0; y < s.H; y++)
            {
                int ty = y0 + y;
                if ((uint)ty >= (uint)H)
                    continue;
                for (int x = 0; x < s.W; x++)
                {
                    Color32 c = s.Px[y * s.W + x];
                    if (c.a != 0)
                        Blend(x0 + x, ty, c, alpha);
                }
            }
        }

        /// <summary>Nearest-neighbour stretch into the rectangle (x, y, w, h).</summary>
        public void BlitScaled(PixelSprite s, float fx, float fy, float fw, float fh)
        {
            int x0 = Mathf.RoundToInt(fx), y0 = Mathf.RoundToInt(fy);
            int w = Mathf.RoundToInt(fw), h = Mathf.RoundToInt(fh);
            if (w <= 0 || h <= 0)
                return;
            for (int y = 0; y < h; y++)
            {
                int sy = Math.Min(s.H - 1, (int)((y + .5f) * s.H / h));
                for (int x = 0; x < w; x++)
                {
                    int sx = Math.Min(s.W - 1, (int)((x + .5f) * s.W / w));
                    Color32 c = s.Px[sy * s.W + sx];
                    if (c.a != 0)
                        Blend(x0 + x, y0 + y, c, 1);
                }
            }
        }

        /// <summary>Same as translate(tx, ty); rotate(r); drawImage(s, ox, oy) on a 2D canvas.</summary>
        public void BlitRotated(PixelSprite s, float tx, float ty, float r, float ox, float oy)
        {
            float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var corner in new[] { new Vector2(ox, oy), new Vector2(ox + s.W, oy), new Vector2(ox, oy + s.H), new Vector2(ox + s.W, oy + s.H) })
            {
                float wx = tx + cos * corner.x - sin * corner.y, wy = ty + sin * corner.x + cos * corner.y;
                minX = Mathf.Min(minX, wx);
                maxX = Mathf.Max(maxX, wx);
                minY = Mathf.Min(minY, wy);
                maxY = Mathf.Max(maxY, wy);
            }

            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
            for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
            {
                float dx = x + .5f - tx, dy = y + .5f - ty;
                int u = Mathf.FloorToInt(cos * dx + sin * dy - ox);
                int v = Mathf.FloorToInt(-sin * dx + cos * dy - oy);
                if ((uint)u >= (uint)s.W || (uint)v >= (uint)s.H)
                    continue;
                Color32 c = s.Px[v * s.W + u];
                if (c.a != 0)
                    Blend(x, y, c, 1);
            }
        }

        public void FillRect(float fx, float fy, float fw, float fh, Color32 c, float alpha = 1)
        {
            int x0 = Mathf.RoundToInt(fx), y0 = Mathf.RoundToInt(fy);
            int w = Math.Max(1, Mathf.RoundToInt(fw)), h = Math.Max(1, Mathf.RoundToInt(fh));
            for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
                Blend(x, y, c, alpha);
        }

        /// <summary>Filled ellipse made of whole pixel rows, matching the prototype's pxEllipse.</summary>
        public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            for (int y = Mathf.CeilToInt(cy - ry); y <= Mathf.FloorToInt(cy + ry); y++)
            {
                float t = (y + .5f - cy) / ry;
                if (Mathf.Abs(t) > 1)
                    continue;
                float hw = rx * Mathf.Sqrt(1 - t * t);
                int x0 = Mathf.RoundToInt(cx - hw);
                int w = Math.Max(1, Mathf.RoundToInt(hw * 2));
                for (int x = x0; x < x0 + w; x++)
                    Blend(x, y, c, 1);
            }
        }
    }
}
