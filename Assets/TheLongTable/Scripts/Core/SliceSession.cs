using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTable.Core
{
    public readonly struct ActionOption
    {
        public readonly ActionDef Def;
        public readonly bool Available;
        public readonly string Reason;

        public ActionOption(ActionDef def, bool available, string reason)
        {
            Def = def; Available = available; Reason = reason;
        }
    }

    public readonly struct ActionResult
    {
        public readonly bool Performed;
        public readonly bool Rebuffed;
        public readonly string FailReason;
        public readonly GameEvent Event;

        public ActionResult(bool performed, bool rebuffed, string failReason, GameEvent e)
        {
            Performed = performed; Rebuffed = rebuffed; FailReason = failReason; Event = e;
        }
    }

    /// <summary>
    /// The rules of one playthrough: resources, what can be done when, and the
    /// log of everything that was done. No scene or rendering code lives here.
    /// </summary>
    public sealed class SliceSession : IDialogueState
    {
        public SliceData Data { get; }
        public EventLog Log { get; }
        public DayLoop Loop { get; }

        public int Bread { get; private set; }
        public int Coin { get; private set; }
        public IReadOnlyList<string> Inventory => inventory;

        private readonly List<string> inventory = new List<string>();
        private readonly HashSet<string> noticedToday = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> doneToday = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> doneEver = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> rebuffedBy = new HashSet<string>(StringComparer.Ordinal);
        private bool remindedToday;

        private MotiveReport cachedReport;
        private int cachedAt = -1;

        public SliceSession(SliceData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Log = new EventLog();
            Loop = new DayLoop(data.Config.attentionPerDay, data.Config.seasonLength);
            Bread = data.Config.startBread;
            Coin = data.Config.startCoin;
        }

        public bool HasNoticed(string villagerId) => noticedToday.Contains(villagerId);
        public bool HasDone(string actionId) => doneEver.Contains(actionId);
        public bool WasRebuffedBy(string villagerId) => rebuffedBy.Contains(villagerId);
        public bool RemindedToday => remindedToday;

        /// <summary>Looking closely at someone during the morning. Free, once per person per day.</summary>
        public bool Notice(string villagerId)
        {
            if (Loop.Phase != DayPhase.Notice) return false;
            if (!Data.TryVillager(villagerId, out var v)) throw new KeyNotFoundException($"No villager '{villagerId}'.");
            if (!noticedToday.Add(villagerId)) return false;

            Log.Append(new GameEvent
            {
                day = Loop.Day, phase = Loop.Phase, kind = EventKind.Noticed,
                target = villagerId, targetCanRepay = v.canRepay, targetOverlooked = v.overlooked,
            });
            return true;
        }

        public IEnumerable<ActionOption> OptionsFor(string villagerId)
        {
            foreach (var a in Data.Actions)
            {
                if (a.target != villagerId) continue;
                if (a.hideAfterRebuff && rebuffedBy.Contains(a.target)) continue;
                if (a.requiresPriorRebuff && !rebuffedBy.Contains(a.target)) continue;
                if (a.requiresNoticed && !noticedToday.Contains(a.target)) continue;
                yield return new ActionOption(a, WhyNot(a) == null, WhyNot(a));
            }
        }

        /// <summary>Null when the action can be done now, otherwise a short reason for the player.</summary>
        public string WhyNot(ActionDef a)
        {
            if (Loop.Phase != DayPhase.Act) return "Not now";
            if (doneToday.Contains(a.id)) return "Already done today";
            if (a.requiresNoticed && !noticedToday.Contains(a.target)) return "You haven't noticed this";
            if (a.requiresPriorRebuff && !rebuffedBy.Contains(a.target)) return "Not yet";
            if (a.costAttention > Loop.AttentionLeft) return "No attention left today";
            if (a.costBread > Bread) return "Not enough bread";
            if (a.costCoin > Coin) return "Not enough coin";
            return null;
        }

        /// <param name="witnesses">Villagers who could see. Ignored for private acts.</param>
        public ActionResult Perform(string actionId, IReadOnlyList<string> witnesses)
        {
            var a = Data.Action(actionId);
            var why = WhyNot(a);
            if (why != null) return new ActionResult(false, false, why, null);

            Loop.TrySpend(a.costAttention);
            Bread -= a.costBread;
            Coin -= a.costCoin;
            doneToday.Add(a.id);
            doneEver.Add(a.id);

            bool rebuffed = a.rebuffsFirstTime && !rebuffedBy.Contains(a.target);
            var target = Data.Villager(a.target);
            var seen = a.Visibility == Visibility.Private || witnesses == null
                ? Array.Empty<string>()
                : witnesses.Where(w => w != a.target && Data.TryVillager(w, out _)).Distinct().ToArray();

            var e = Log.Append(new GameEvent
            {
                day = Loop.Day, phase = Loop.Phase, kind = EventKind.Acted,
                target = a.target, actionId = a.id, family = a.Family, visibility = a.Visibility,
                witnesses = seen,
                costBread = a.costBread, costCoin = a.costCoin, costAttention = a.costAttention,
                rewardRequested = a.rewardRequested, rewardItem = a.rewardItem,
                targetCanRepay = target.canRepay, targetOverlooked = target.overlooked,
                wasRebuffed = rebuffed,
            });

            if (rebuffed)
            {
                rebuffedBy.Add(a.target);
                Log.Append(new GameEvent
                {
                    day = Loop.Day, phase = Loop.Phase, kind = EventKind.Rebuffed,
                    actor = a.target, target = "player", actionId = a.id,
                });
            }
            else if (a.rewardRequested && !string.IsNullOrEmpty(a.rewardItem))
            {
                inventory.Add(a.rewardItem);
            }

            return new ActionResult(true, rebuffed, null, e);
        }

        /// <summary>Telling the table what you did today. Allowed once, at Gather.</summary>
        public bool Remind()
        {
            if (Loop.Phase != DayPhase.Gather || remindedToday) return false;
            if (!Log.Events.Any(e => e.IsPlayerAct && e.day == Loop.Day)) return false;
            remindedToday = true;
            Log.Append(new GameEvent { day = Loop.Day, phase = Loop.Phase, kind = EventKind.Reminded, target = "table" });
            return true;
        }

        public void AdvancePhase()
        {
            var from = Loop.Phase;
            Loop.Advance();
            Log.Append(new GameEvent
            {
                day = Loop.Day, phase = Loop.Phase, kind = EventKind.PhaseChanged, note = $"{from}->{Loop.Phase}",
            });
        }

        public void StartNextDay()
        {
            Loop.StartNextDay();
            noticedToday.Clear();
            doneToday.Clear();
            remindedToday = false;
            Log.Append(new GameEvent { day = Loop.Day, phase = Loop.Phase, kind = EventKind.PhaseChanged, note = "DayEnd->Notice" });
        }

        public MotiveReport Evaluate()
        {
            if (cachedReport == null || cachedAt != Log.Count)
            {
                cachedReport = MotiveEvaluator.Evaluate(Log, Data);
                cachedAt = Log.Count;
            }
            return cachedReport;
        }

        // IDialogueState
        public string Tier => Evaluate().Tier;
        public float TrustOf(string villagerId) => Evaluate().TrustOf(villagerId);
    }
}
