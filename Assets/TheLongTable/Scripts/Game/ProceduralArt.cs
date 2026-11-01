using System;
using System.Collections.Generic;
using UnityEngine;

namespace LongTable.Game
{
    /// <summary>
    /// Placeholder shapes drawn at runtime: white sprites with soft edges that the
    /// lighting system tints. Painted art replaces these later without code changes
    /// to the systems that use them.
    /// </summary>
    public static class ProceduralArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        /// <summary>1 x 1 unit, centered.</summary>
        public static Sprite Rect => Cached("rect", () => FromAlpha(8, 8, (u, v, px) => 1f, Center));

        /// <summary>1 x 1 unit, pivot at bottom center.</summary>
        public static Sprite RectBottom => Cached("rectBottom", () => FromAlpha(8, 8, (u, v, px) => 1f, Bottom));

        /// <summary>1 unit across, centered.</summary>
        public static Sprite Circle => Cached("circle", () =>
            FromAlpha(64, 64, (u, v, px) => Clamp01((0.5f - Dist(u, v, 0.5f, 0.5f)) * px), Center));

        /// <summary>Soft radial light, 1 unit across.</summary>
        public static Sprite Glow => Cached("glow", () => FromAlpha(128, 128, (u, v, px) =>
        {
            float d = Clamp01(Dist(u, v, 0.5f, 0.5f) / 0.5f);
            float a = 1f - d;
            return a * a * (3f - 2f * a);
        }, Center));

        /// <summary>1 x 1 unit, bottom pivot, apex at the top.</summary>
        public static Sprite Triangle => Cached("triangle", () => FromAlpha(128, 128, (u, v, px) =>
            Clamp01((0.5f * (1f - v) - Mathf.Abs(u - 0.5f)) * px), Bottom));

        /// <summary>Half circle, 1 x 0.5 units, flat side down.</summary>
        public static Sprite Dome => Cached("dome", () => FromAlpha(64, 32, (u, v, px) =>
        {
            float x = u - 0.5f, y = v * 0.5f;
            return Clamp01((0.5f - Mathf.Sqrt(x * x + y * y)) * px);
        }, Bottom));

        /// <summary>1 x 1 unit, opaque at the top and clear at the bottom.</summary>
        public static Sprite SkyFade => Cached("skyFade", () => FromAlpha(64, 64, (u, v, px) =>
        {
            float t = Clamp01(v);
            return t * t * (3f - 2f * t);
        }, Bottom));

        /// <summary>Rounded body shape, 1 x 2 units, bottom pivot.</summary>
        public static Sprite Body => Cached("body", () => FromAlpha(64, 128, (u, v, px) =>
        {
            // Rounded rectangle in a 1 x 2 space with shoulders rounder than the hem.
            float x = (u - 0.5f), y = v * 2f;
            float r = y > 1f ? 0.42f : 0.18f;
            float hx = 0.5f - r, hy0 = r, hy1 = 2f - r;
            float qx = Mathf.Max(Mathf.Abs(x) - hx, 0f);
            float cy = Mathf.Clamp(y, hy0, hy1);
            float qy = Mathf.Abs(y - cy);
            float sdf = r - Mathf.Sqrt(qx * qx + qy * qy);
            return Clamp01(sdf * px);
        }, Bottom));

        /// <summary>A silhouette strip for mountains and hills. Same seed, same shape.</summary>
        public static Sprite Ridge(int seed, float widthUnits, float heightUnits, float roughness, float baseHeight)
        {
            string key = $"ridge:{seed}:{widthUnits}:{heightUnits}:{roughness}:{baseHeight}";
            return Cached(key, () =>
            {
                const int ppu = 24;
                int w = Mathf.Max(8, Mathf.RoundToInt(widthUnits * ppu));
                int h = Mathf.Max(8, Mathf.RoundToInt(heightUnits * ppu));
                var rng = new System.Random(seed);
                float[] freq = { 1.1f, 2.3f, 5.3f, 11.7f, 23.1f };
                float[] amp = { 0.42f, 0.24f, 0.13f * roughness, 0.06f * roughness, 0.03f * roughness };
                float[] phase = new float[freq.Length];
                for (int i = 0; i < phase.Length; i++) phase[i] = (float)rng.NextDouble() * Mathf.PI * 2f;

                var heights = new float[w];
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float sum = 0f, norm = 0f;
                    for (int i = 0; i < freq.Length; i++)
                    {
                        sum += amp[i] * Mathf.Sin(u * Mathf.PI * 2f * freq[i] + phase[i]);
                        norm += amp[i];
                    }
                    float n = 0.5f + 0.5f * sum / Mathf.Max(norm, 0.0001f);
                    heights[x] = (baseHeight + (1f - baseHeight) * n) * h;
                }

                return FromPixels(w, h, (x, y) => Clamp01(heights[x] - y), Bottom, ppu);
            });
        }

        private static Sprite Cached(string key, Func<Sprite> make)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null) Cache[key] = s = make();
            return s;
        }

        /// <summary>alpha(u, v, pixelsAcross) with u, v at pixel centers in 0..1. Sprite is 1 unit wide.</summary>
        private static Sprite FromAlpha(int w, int h, Func<float, float, float, float> alpha, Vector2 pivot)
        {
            return FromPixels(w, h, (x, y) => alpha((x + 0.5f) / w, (y + 0.5f) / h, w), pivot, w);
        }

        private static Sprite FromPixels(int w, int h, Func<int, int, float> alpha, Vector2 pivot, float ppu)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Clamp01(alpha(x, y)) * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static float Dist(float x0, float y0, float x1, float y1)
        {
            float dx = x0 - x1, dy = y0 - y1;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }
}
