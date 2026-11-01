using System;
using System.Collections;
using System.IO;
using LongTable.Core;
using LongTable.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LongTable.Tests
{
    /// <summary>
    /// Plays both reference days in the real scene and checks the difference is
    /// visible on screen, not just in numbers. Saves frames to Screenshots/.
    /// Needs a graphics device: run without -nographics.
    /// </summary>
    public sealed class SliceVisualTests
    {
        private sealed class DayResult
        {
            public MotiveReport Report;
            public OvenOutcome? ShownOven;
            public float DuskSaturation, MorningSaturation, MorningBrightness;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator SincereAndTransactionalDaysLookDifferent()
        {
            var sincere = new DayResult();
            yield return Play("sincere", sincere);
            var transactional = new DayResult();
            yield return Play("transactional", transactional);

            Debug.Log($"[LongTable] sincere: warmth {sincere.Report.Warmth:0.0}, dusk sat {sincere.DuskSaturation:0.000}, " +
                      $"morning sat {sincere.MorningSaturation:0.000}, bright {sincere.MorningBrightness:0.000}, oven {sincere.ShownOven}");
            Debug.Log($"[LongTable] transactional: warmth {transactional.Report.Warmth:0.0}, dusk sat {transactional.DuskSaturation:0.000}, " +
                      $"morning sat {transactional.MorningSaturation:0.000}, bright {transactional.MorningBrightness:0.000}, oven {transactional.ShownOven}");

            Assert.AreEqual(OvenOutcome.Relit, sincere.ShownOven);
            Assert.AreEqual(OvenOutcome.Polite, transactional.ShownOven);
            Assert.That(sincere.Report.Warmth, Is.GreaterThan(transactional.Report.Warmth + 15f));
            Assert.That(sincere.DuskSaturation, Is.GreaterThan(transactional.DuskSaturation + 0.05f), "Dusk should look warmer.");
            Assert.That(sincere.MorningSaturation, Is.GreaterThan(transactional.MorningSaturation + 0.05f), "Morning should look warmer.");
        }

        private static IEnumerator Play(string play, DayResult result)
        {
            var go = new GameObject("Slice under test");
            var boot = go.AddComponent<SliceBootstrap>();
            boot.instant = true;
            yield return null;

            var director = boot.Director;
            Assert.IsNotNull(director, "Bootstrap did not create a director.");
            director.StartAutoplay(play);

            yield return Until(() => director.IsAwaitingGatherChoice, $"{play}: never reached the table");
            yield return null;
            result.DuskSaturation = Capture(director, $"{play}_1_dusk", includeUi: true).saturation;

            yield return Until(() => director.IsFinished, $"{play}: day never finished");
            yield return null;
            var world = Capture(director, $"{play}_2_morning", includeUi: false);
            result.MorningSaturation = world.saturation;
            result.MorningBrightness = world.brightness;
            Capture(director, $"{play}_3_debrief", includeUi: true);

            result.Report = director.Report;
            result.ShownOven = director.World.Oven.Outcome;

            var cam = director.Camera;
            UnityEngine.Object.Destroy(go);
            if (cam != null) UnityEngine.Object.Destroy(cam.gameObject);
            yield return null;
        }

        private static IEnumerator Until(Func<bool> condition, string failure, float timeout = 30f)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail(failure);
                yield return null;
            }
        }

        private static (float saturation, float brightness) Capture(SliceDirector director, string name, bool includeUi)
        {
            var cam = director.Camera;
            var canvas = director.GetComponentInChildren<Canvas>(true);
            if (canvas != null) canvas.enabled = includeUi;

            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;

            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            if (canvas != null) canvas.enabled = true;

            float sat = 0f, bright = 0f;
            var px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i += 7)
            {
                Color.RGBToHSV(px[i], out _, out var s, out var v);
                sat += s;
                bright += v;
            }
            int n = (px.Length + 6) / 7;

            string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Screenshots");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());

            UnityEngine.Object.Destroy(tex);
            rt.Release();
            UnityEngine.Object.Destroy(rt);
            return (sat / n, bright / n);
        }
    }
}
