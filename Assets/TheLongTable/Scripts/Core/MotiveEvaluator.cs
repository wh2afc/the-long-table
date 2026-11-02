using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LongTable.Core
{
    public enum OvenOutcome { Cold, Polite, Relit }

    public enum Seat { Apart, Edge, Close }

    public sealed class SignalResult
    {
        public string Id;
        public float Score;     // 0 = transactional, 1 = sincere
        public int Evidence;    // how many events informed it; 0 means it was skipped
        public float Weight;
        public string Explanation;
    }

    /// <summary>The evaluator's full answer. Deterministic for a given log and rule set.</summary>
    public sealed class MotiveReport
    {
        public float Sincerity;
        public float Warmth;
        public string Tier;
        public int Acts;
        public int Reminders;
        public IReadOnlyList<SignalResult> Signals;
        public IReadOnlyDictionary<string, float> Trust;
        public IReadOnlyDictionary<string, Seat> Seats;
        public OvenOutcome Oven;
        public IReadOnlyList<string> RewardsReceived;

        public float TrustOf(string villagerId) => Trust.TryGetValue(villagerId, out var t) ? t : 0f;
        public Seat SeatOf(string villagerId) => Seats.TryGetValue(villagerId, out var s) ? s : Seat.Apart;

        public string Describe()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Warmth {Warmth:0.0} ({Tier}), sincerity {Sincerity:0.00}, {Acts} acts, {Reminders} reminders");
            foreach (var s in Signals)
                sb.AppendLine(s.Evidence == 0
                    ? $"  {s.Id}: skipped ({s.Explanation})"
                    : $"  {s.Id}: {s.Score:0.00} x{s.Weight:0.##} ({s.Explanation})");
            foreach (var kv in Trust.OrderBy(k => k.Key, StringComparer.Ordinal))
                sb.AppendLine($"  trust {kv.Key}: {kv.Value:0.0} -> {SeatOf(kv.Key)}");
            sb.Append($"  oven: {Oven}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Reads patterns in the event log and turns them into a sincerity score,
    /// village warmth, per-villager trust, seating and the oven outcome.
    /// Rule-based and deterministic: no randomness, no clocks, no scene state.
    /// </summary>
    public static class MotiveEvaluator
    {
        public static MotiveReport Evaluate(EventLog log, SliceData data)
        {
            var rules = data.Rules;
            var events = log.Events;
            var acts = events.Where(e => e.IsPlayerAct).ToList();
            var reminders = events.Where(e => e.kind == EventKind.Reminded && e.actor == "player").ToList();
            var rebuffs = events.Where(e => e.kind == EventKind.Rebuffed).ToList();

            var signals = new List<SignalResult>();
            foreach (var sw in rules.signals)
            {
                var r = ComputeSignal(sw.id, rules, events, acts, reminders, rebuffs);
                r.Weight = sw.weight;
                signals.Add(r);
            }

            float weightSum = 0f, scoreSum = 0f;
            foreach (var s in signals)
            {
                if (s.Evidence == 0 || s.Weight <= 0f) continue;
                weightSum += s.Weight;
                scoreSum += s.Weight * s.Score;
            }
            float sincerity = weightSum > 0f ? scoreSum / weightSum : rules.neutralSincerity;
            float multiplier = Lerp(rules.minMultiplier, 1f, sincerity);

            float earned = 0f;
            var trust = data.Villagers.ToDictionary(v => v.id, v => 0f, StringComparer.Ordinal);
            foreach (var act in acts)
            {
                float baseWarmth = data.TryAction(act.actionId, out var def) ? def.baseWarmth : 0f;
                float actValue = act.wasRebuffed ? baseWarmth * rules.act.rebuffedAttemptValue : baseWarmth;
                earned += actValue;

                if (trust.ContainsKey(act.target))
                    trust[act.target] += baseWarmth * ActQuality(act, rules, events) * multiplier;
            }

            float warmth = rules.startWarmth + earned * multiplier - rules.remindPenalty * reminders.Count;
            warmth = Clamp(warmth, 0f, rules.maxWarmth);

            var seats = new Dictionary<string, Seat>(StringComparer.Ordinal);
            foreach (var kv in trust)
            {
                seats[kv.Key] = kv.Value >= rules.seats.closeMinTrust ? Seat.Close
                              : kv.Value >= rules.seats.edgeMinTrust ? Seat.Edge
                              : Seat.Apart;
            }

            return new MotiveReport
            {
                Sincerity = sincerity,
                Warmth = warmth,
                Tier = TierFor(warmth, rules),
                Acts = acts.Count,
                Reminders = reminders.Count,
                Signals = signals,
                Trust = trust,
                Seats = seats,
                Oven = OvenFor(acts, trust, sincerity, rules),
                RewardsReceived = acts.Where(a => a.rewardRequested && !string.IsNullOrEmpty(a.rewardItem))
                                      .Select(a => a.rewardItem).ToList(),
            };
        }

        public static string TierFor(float warmth, MotiveRules rules)
        {
            string best = rules.tiers.Length > 0 ? rules.tiers[0].name : "";
            float bestMin = float.MinValue;
            foreach (var t in rules.tiers)
            {
                if (warmth >= t.minWarmth && t.minWarmth >= bestMin) { best = t.name; bestMin = t.minWarmth; }
            }
            return best;
        }

        /// <summary>How well one act lands with the person it was for, 0 to maxQuality.</summary>
        private static float ActQuality(GameEvent act, MotiveRules rules, IReadOnlyList<GameEvent> events)
        {
            var q = rules.act;
            float quality = 1f;

            if (act.rewardRequested) quality -= q.rewardRequestedPenalty;

            // "Someone to notice, not rescue loudly": public help to the overlooked costs them pride.
            if (act.visibility == Visibility.Public && act.targetOverlooked) quality -= q.loudRescuePenalty;

            if (events.Any(e => e.kind == EventKind.Reminded && e.actor == "player" && e.seq > act.seq))
                quality -= q.remindedPenalty;

            if (IsReturnAfterRebuff(act, events)) quality += q.returnAfterRebuffBonus;

            quality = Clamp(quality, 0f, q.maxQuality);
            if (act.wasRebuffed) quality *= q.rebuffedAttemptValue;
            return quality;
        }

        private static bool IsReturnAfterRebuff(GameEvent act, IReadOnlyList<GameEvent> events) =>
            !act.wasRebuffed && events.Any(e => e.kind == EventKind.Rebuffed && e.actor == act.target && e.seq < act.seq);

        private static SignalResult ComputeSignal(string id, MotiveRules rules, IReadOnlyList<GameEvent> events,
            List<GameEvent> acts, List<GameEvent> reminders, List<GameEvent> rebuffs)
        {
            int n = acts.Count;
            switch (id)
            {
                case SignalIds.PrivateHelp:
                {
                    int unseen = acts.Count(a => !a.Witnessed);
                    float share = n == 0 ? 0f : (float)unseen / n;
                    return new SignalResult
                    {
                        Id = id, Evidence = n,
                        Score = n == 0 ? 0f : Clamp01(share / Max(rules.privateShareForFull, 0.01f)),
                        Explanation = n == 0 ? "no acts" : $"{unseen} of {n} acts had no witnesses",
                    };
                }
                case SignalIds.NoReminding:
                {
                    int reminded = acts.Count(a => reminders.Any(r => r.seq > a.seq));
                    return new SignalResult
                    {
                        Id = id, Evidence = n,
                        Score = n == 0 ? 0f : 1f - (float)reminded / n,
                        Explanation = n == 0 ? "no acts" : $"{reminded} of {n} acts were pointed out afterwards",
                    };
                }
                case SignalIds.CannotRepay:
                {
                    int forThem = acts.Count(a => !a.targetCanRepay || a.targetOverlooked);
                    float share = n == 0 ? 0f : (float)forThem / n;
                    return new SignalResult
                    {
                        Id = id, Evidence = n,
                        Score = n == 0 ? 0f : Clamp01(share / Max(rules.cannotRepayShareForFull, 0.01f)),
                        Explanation = n == 0 ? "no acts" : $"{forThem} of {n} acts were for someone who can't repay",
                    };
                }
                case SignalIds.StaysThroughRebuff:
                {
                    int returned = rebuffs.Count(r => acts.Any(a => a.target == r.actor && a.seq > r.seq && !a.wasRebuffed));
                    return new SignalResult
                    {
                        Id = id, Evidence = rebuffs.Count,
                        Score = rebuffs.Count == 0 ? 0f : (float)returned / rebuffs.Count,
                        Explanation = rebuffs.Count == 0 ? "nobody turned you away" : $"came back after {returned} of {rebuffs.Count} rebuffs",
                    };
                }
                case SignalIds.NoRewardAsked:
                {
                    int free = acts.Count(a => !a.rewardRequested);
                    return new SignalResult
                    {
                        Id = id, Evidence = n,
                        Score = n == 0 ? 0f : (float)free / n,
                        Explanation = n == 0 ? "no acts" : $"{free} of {n} acts asked nothing back",
                    };
                }
                case SignalIds.NoticedFirst:
                {
                    int noticed = acts.Count(a => events.Any(e =>
                        e.kind == EventKind.Noticed && e.target == a.target && e.day == a.day && e.seq < a.seq));
                    return new SignalResult
                    {
                        Id = id, Evidence = n,
                        Score = n == 0 ? 0f : (float)noticed / n,
                        Explanation = n == 0 ? "no acts" : $"{noticed} of {n} acts followed noticing that person",
                    };
                }
                default:
                    throw new ArgumentException($"Unknown motive signal '{id}'.");
            }
        }

        private static OvenOutcome OvenFor(List<GameEvent> acts, Dictionary<string, float> trust, float sincerity, MotiveRules rules)
        {
            var o = rules.oven;
            var forOwner = acts.Where(a => a.target == o.owner).ToList();
            if (forOwner.Count == 0) return OvenOutcome.Cold;

            Enum.TryParse(o.relitRequiresFamily, out ActionFamily needed);
            bool sat = forOwner.Any(a => a.family == needed && !a.wasRebuffed);
            float t = trust.TryGetValue(o.owner, out var v) ? v : 0f;
            return sat && t >= o.relitMinTrust && sincerity >= o.relitMinSincerity ? OvenOutcome.Relit : OvenOutcome.Polite;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        private static float Clamp01(float v) => Clamp(v, 0f, 1f);
        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
        private static float Max(float a, float b) => a > b ? a : b;
    }
}
