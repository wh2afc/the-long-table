using System.Collections.Generic;
using UnityEngine;

namespace LongTable.Game
{
    public enum TintMode { Lit, Emissive, SkyTop, SkyBottom, Fixed }

    /// <summary>A sprite whose color comes from the world look rather than being set directly.</summary>
    public sealed class Tinted : MonoBehaviour
    {
        public Color baseColor = Color.white;
        public TintMode mode = TintMode.Lit;
        [Range(0f, 1f)] public float haze;
        public float strength = 1f;        // emissive only
        public float emissiveFloor;        // emissive only: minimum glow regardless of time of day
        public float alphaScale = 1f;

        public SpriteRenderer Renderer { get; private set; }

        private void Awake() => Renderer = GetComponent<SpriteRenderer>();

        public void Apply(WorldLook look)
        {
            if (Renderer == null) Renderer = GetComponent<SpriteRenderer>();
            Color c;
            switch (mode)
            {
                case TintMode.Lit:
                    c = look.Grade(baseColor);
                    if (haze > 0f) c = Color.Lerp(c, look.Haze, haze);
                    c.a = baseColor.a;
                    break;
                case TintMode.Emissive:
                    c = look.GradeEmissive(baseColor);
                    c.a = baseColor.a * strength * Mathf.Max(emissiveFloor, look.Emissive);
                    break;
                case TintMode.SkyTop:
                    c = look.Zenith;
                    break;
                case TintMode.SkyBottom:
                    c = look.Horizon;
                    break;
                default:
                    c = baseColor;
                    break;
            }
            c.a *= alphaScale;
            Renderer.color = c;
        }
    }

    /// <summary>
    /// Turns time of day and village warmth into colour. A cold village is grey
    /// and cool; a warm one is saturated, golden at dusk, with brighter windows.
    /// Painted art and URP 2D lights can replace the tinting later behind the same inputs.
    /// </summary>
    public sealed class WorldLook : MonoBehaviour
    {
        // Keyframes at time-of-day 0 (morning), 1 (midday), 2 (dusk), 3 (night).
        private static readonly Color[] ZenithKeys = { Hex("#9FB3C6"), Hex("#7EA5CB"), Hex("#423C6B"), Hex("#1B1E38") };
        private static readonly Color[] HorizonKeys = { Hex("#F1D2B0"), Hex("#CADCE5"), Hex("#EE9A6A"), Hex("#4B3C5E") };
        private static readonly Color[] AmbientKeys =
        {
            new Color(1.00f, 0.96f, 0.92f), new Color(1f, 1f, 1f), new Color(0.80f, 0.66f, 0.68f), new Color(0.46f, 0.46f, 0.62f),
        };
        private static readonly float[] EmissiveKeys = { 0.2f, 0f, 1f, 1f };

        private static readonly Color MutedTint = new Color(0.93f, 0.97f, 1.04f);
        private static readonly Color PulseTint = new Color(1.10f, 1.00f, 0.86f);

        private readonly List<Tinted> tinted = new List<Tinted>();

        public bool Instant;

        public float TimeOfDay { get; private set; } = 0.2f;
        public float TargetTime = 0.2f;
        public float Warmth01 { get; private set; }
        public float TargetWarmth01;
        public float Pulse { get; private set; }
        public float TargetPulse;

        public Color Zenith { get; private set; }
        public Color Horizon { get; private set; }
        public Color Ambient { get; private set; }
        public Color Haze { get; private set; }
        public float Emissive { get; private set; }
        public float Saturation { get; private set; }

        public void Register(Tinted t)
        {
            tinted.Add(t);
            t.Apply(this);
        }

        public void Snap()
        {
            TimeOfDay = TargetTime;
            Warmth01 = TargetWarmth01;
            Pulse = TargetPulse;
            Refresh();
        }

        private void LateUpdate()
        {
            if (Instant)
            {
                Snap();
                return;
            }
            float dt = Time.deltaTime;
            TimeOfDay = Mathf.MoveTowards(TimeOfDay, TargetTime, dt * 0.45f);
            Warmth01 = Mathf.MoveTowards(Warmth01, TargetWarmth01, dt * 0.18f);
            Pulse = Mathf.MoveTowards(Pulse, TargetPulse, dt * 0.35f);
            Refresh();
        }

        public void Refresh()
        {
            float t = Mathf.Clamp(TimeOfDay, 0f, 3f);
            int i = Mathf.Min(2, Mathf.FloorToInt(t));
            float f = Smooth(t - i);

            Zenith = Color.Lerp(ZenithKeys[i], ZenithKeys[i + 1], f);
            Horizon = Color.Lerp(HorizonKeys[i], HorizonKeys[i + 1], f);
            Color ambient = Color.Lerp(AmbientKeys[i], AmbientKeys[i + 1], f);
            float emissive = Mathf.Lerp(EmissiveKeys[i], EmissiveKeys[i + 1], f);

            float w = Warmth01;
            Saturation = Mathf.Lerp(0.25f, 1.05f, w) + Pulse * 0.15f;

            float bright = Mathf.Lerp(0.88f, 1.03f, w);
            ambient = Mul(ambient, new Color(bright, bright, bright));
            ambient = Mul(ambient, Color.Lerp(Color.white, PulseTint, Pulse));
            Ambient = ambient;

            // The sky also loses colour when the village is cold.
            Zenith = Desaturate(Zenith, Mathf.Lerp(0.45f, 1f, w));
            Horizon = Desaturate(Horizon, Mathf.Lerp(0.40f, 1f, w) + Pulse * 0.1f);
            Horizon = Color.Lerp(Horizon, Mul(Horizon, PulseTint), Pulse);

            Haze = Color.Lerp(Horizon, Zenith, 0.35f);
            Emissive = emissive * Mathf.Lerp(0.5f, 1f, w) + Pulse * 0.3f;

            for (int k = tinted.Count - 1; k >= 0; k--)
            {
                if (tinted[k] == null) { tinted.RemoveAt(k); continue; }
                tinted[k].Apply(this);
            }
        }

        public Color Grade(Color c)
        {
            Color graded = Desaturate(c, Saturation);
            graded = Color.Lerp(Mul(graded, MutedTint), graded, Warmth01);
            return Mul(graded, Ambient);
        }

        public Color GradeEmissive(Color c) => Desaturate(c, Mathf.Lerp(0.55f, 1f, Warmth01));

        private static Color Desaturate(Color c, float saturation)
        {
            float g = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            var gray = new Color(g, g, g, c.a);
            var r = Color.LerpUnclamped(gray, c, saturation);
            r.r = Mathf.Clamp01(r.r); r.g = Mathf.Clamp01(r.g); r.b = Mathf.Clamp01(r.b);
            return r;
        }

        private static Color Mul(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);

        private static float Smooth(float x) => x * x * (3f - 2f * x);

        private static Color Hex(string h) => ProceduralArt.Hex(h);
    }
}
