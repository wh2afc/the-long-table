using System.Collections.Generic;
using LongTable.Core;
using UnityEngine;

namespace LongTable.Game
{
    /// <summary>Marta's oven: the one scene in the slice that changes with how you played.</summary>
    public sealed class OvenView : MonoBehaviour
    {
        public Tinted MouthGlow;
        public Tinted Halo;
        public Tinted WindowGlow;
        public readonly List<Tinted> Loaves = new List<Tinted>();
        public readonly List<Tinted> KneadingTable = new List<Tinted>();
        public readonly List<Tinted> Smoke = new List<Tinted>();
        public Vector2 ChimneyTop;
        public float KneadingX;
        public bool Instant;

        public OvenOutcome? Outcome { get; private set; }

        private int activePuffs;
        private float smokeAlpha;

        public void SetOutcome(OvenOutcome? outcome)
        {
            Outcome = outcome;
            var o = outcome ?? OvenOutcome.Cold;
            MouthGlow.strength = o == OvenOutcome.Relit ? 1f : o == OvenOutcome.Polite ? 0.55f : 0f;
            Halo.strength = o == OvenOutcome.Relit ? 0.95f : o == OvenOutcome.Polite ? 0.2f : 0f;
            WindowGlow.strength = o == OvenOutcome.Relit ? 1f : o == OvenOutcome.Polite ? 0.35f : 0.08f;
            foreach (var l in Loaves) l.alphaScale = o == OvenOutcome.Polite ? 1f : 0f;
            foreach (var k in KneadingTable) k.alphaScale = o == OvenOutcome.Relit ? 1f : 0f;
            activePuffs = o == OvenOutcome.Relit ? Smoke.Count : o == OvenOutcome.Polite ? 2 : 0;
            if (Instant) smokeAlpha = activePuffs > 0 ? 1f : 0f;
        }

        private void Update()
        {
            smokeAlpha = Mathf.MoveTowards(smokeAlpha, activePuffs > 0 ? 1f : 0f, Time.deltaTime * 0.5f);
            for (int i = 0; i < Smoke.Count; i++)
            {
                var puff = Smoke[i];
                float life = Mathf.Repeat(Time.time * 0.16f + i / (float)Smoke.Count, 1f);
                puff.transform.position = new Vector3(
                    ChimneyTop.x + Mathf.Sin(life * 5f + i) * 0.25f + life * 0.9f,
                    ChimneyTop.y + life * 3.2f, 0f);
                float s = Mathf.Lerp(0.35f, 1.3f, life);
                puff.transform.localScale = new Vector3(s, s, 1f);
                bool on = i < activePuffs;
                puff.alphaScale = on ? smokeAlpha * Mathf.Sin(life * Mathf.PI) * 0.55f : 0f;
            }
        }
    }
}
