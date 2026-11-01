using System;
using System.IO;
using System.Linq;
using LongTable.Core;
using NUnit.Framework;

namespace LongTable.Tests
{
    public sealed class DayLoopTests
    {
        [Test]
        public void PhasesRunNoticeActGatherDayEnd()
        {
            var loop = new DayLoop(3, 7);
            Assert.AreEqual(DayPhase.Notice, loop.Phase);
            loop.Advance(); Assert.AreEqual(DayPhase.Act, loop.Phase);
            loop.Advance(); Assert.AreEqual(DayPhase.Gather, loop.Phase);
            loop.Advance(); Assert.AreEqual(DayPhase.DayEnd, loop.Phase);
            Assert.Throws<InvalidOperationException>(() => loop.Advance());
        }

        [Test]
        public void FeastComesAtTheEndOfTheSeason()
        {
            var loop = new DayLoop(3, 2);
            for (int i = 0; i < 3; i++) loop.Advance();
            loop.StartNextDay();
            Assert.AreEqual(2, loop.Day);
            loop.Advance(); loop.Advance(); loop.Advance();
            Assert.AreEqual(DayPhase.Feast, loop.Phase);
            loop.Advance();
            Assert.AreEqual(DayPhase.DayEnd, loop.Phase);
        }

        [Test]
        public void AttentionIsLimitedAndOnlySpentAtMidday()
        {
            var loop = new DayLoop(2, 7);
            Assert.IsFalse(loop.TrySpend(1), "No acting during the morning.");
            loop.Advance();
            Assert.IsTrue(loop.TrySpend(1));
            Assert.IsTrue(loop.TrySpend(1));
            Assert.IsFalse(loop.TrySpend(1));
            Assert.AreEqual(0, loop.AttentionLeft);
        }

        [Test]
        public void NewDayRefillsAttentionAndNeedsTheDayToBeOver()
        {
            var loop = new DayLoop(2, 7);
            Assert.Throws<InvalidOperationException>(() => loop.StartNextDay());
            loop.Advance(); loop.TrySpend(2); loop.Advance(); loop.Advance();
            loop.StartNextDay();
            Assert.AreEqual(2, loop.AttentionLeft);
            Assert.AreEqual(DayPhase.Notice, loop.Phase);
        }
    }

    public sealed class SessionTests
    {
        private SliceData data;
        private SliceSession s;

        [SetUp]
        public void Setup()
        {
            data = SliceData.LoadFromResources();
            s = new SliceSession(data);
        }

        [Test]
        public void QuietBreadForPipNeedsNoticingFirst()
        {
            s.AdvancePhase();
            Assert.IsFalse(s.OptionsFor("pip").Any(o => o.Def.id == "pip_bread_quiet"), "There is no quest marker for Pip.");
            Assert.IsTrue(s.OptionsFor("pip").Any(o => o.Def.id == "pip_bread_public"));

            var noticed = new SliceSession(data);
            noticed.Notice("pip");
            noticed.AdvancePhase();
            Assert.IsTrue(noticed.OptionsFor("pip").Any(o => o.Def.id == "pip_bread_quiet" && o.Available));
        }

        [Test]
        public void TobiasRebuffsTheFirstOfferThenYouCanComeBack()
        {
            s.AdvancePhase();
            Assert.IsFalse(s.OptionsFor("tobias").Any(o => o.Def.id == "tobias_return"));
            var r = s.Perform("tobias_offer_help", new[] { "pip" });
            Assert.IsTrue(r.Performed);
            Assert.IsTrue(r.Rebuffed);
            Assert.IsTrue(s.Log.Events.Any(e => e.kind == EventKind.Rebuffed && e.actor == "tobias"));
            Assert.IsFalse(s.OptionsFor("tobias").Any(o => o.Def.id == "tobias_offer_help"));
            Assert.IsTrue(s.OptionsFor("tobias").Any(o => o.Def.id == "tobias_return" && o.Available));
        }

        [Test]
        public void ActsCostResourcesAndAttention()
        {
            s.AdvancePhase();
            int coin = s.Coin, attention = s.Loop.AttentionLeft;
            Assert.IsTrue(s.Perform("tobias_trade", null).Performed);
            Assert.AreEqual(coin - 2, s.Coin);
            Assert.AreEqual(attention - 1, s.Loop.AttentionLeft);
            CollectionAssert.Contains(s.Inventory, "hoe");
            Assert.AreEqual("Already done today", s.Perform("tobias_trade", null).FailReason);
        }

        [Test]
        public void YouRunOutOfAttention()
        {
            s.Notice("pip"); s.Notice("marta");
            s.AdvancePhase();
            Assert.IsTrue(s.Perform("pip_bread_quiet", null).Performed);
            Assert.IsTrue(s.Perform("marta_sit", null).Performed);
            Assert.IsTrue(s.Perform("pip_listen", null).Performed);
            Assert.IsTrue(s.Perform("marta_fix_flue", null).Performed);
            var r = s.Perform("tobias_offer_help", null);
            Assert.IsFalse(r.Performed);
            Assert.AreEqual("No attention left today", r.FailReason);
        }

        [Test]
        public void PrivateActsRecordNoWitnessesEvenIfPeopleAreNear()
        {
            s.Notice("pip");
            s.AdvancePhase();
            var e = s.Perform("pip_bread_quiet", new[] { "marta", "tobias" }).Event;
            Assert.IsFalse(e.Witnessed);

            var pub = s.Perform("marta_fix_flue", new[] { "marta", "pip" }).Event;
            CollectionAssert.AreEqual(new[] { "pip" }, pub.witnesses, "The person being helped is not a witness.");
        }

        [Test]
        public void YouCanOnlyRemindAtGatherAfterDoingSomething()
        {
            Assert.IsFalse(s.Remind());
            s.AdvancePhase(); s.AdvancePhase();
            Assert.IsFalse(s.Remind(), "Nothing to boast about.");

            var busy = new SliceSession(data);
            busy.AdvancePhase();
            busy.Perform("pip_bread_public", null);
            busy.AdvancePhase();
            Assert.IsTrue(busy.Remind());
            Assert.IsFalse(busy.Remind(), "Once a day.");
        }

        [Test]
        public void NoticingIsOncePerPersonAndOnlyInTheMorning()
        {
            Assert.IsTrue(s.Notice("marta"));
            Assert.IsFalse(s.Notice("marta"));
            s.AdvancePhase();
            Assert.IsFalse(s.Notice("pip"));
        }
    }

    public sealed class EventLogTests
    {
        [Test]
        public void AppendAssignsSequenceNumbers()
        {
            var log = new EventLog();
            log.Append(new GameEvent { kind = EventKind.Noticed, target = "pip" });
            log.Append(new GameEvent { kind = EventKind.Acted, target = "pip" });
            Assert.AreEqual(new[] { 0, 1 }, log.Events.Select(e => e.seq).ToArray());
        }

        [Test]
        public void RoundTripsThroughJson()
        {
            var log = new EventLog();
            log.Append(new GameEvent
            {
                day = 1, phase = DayPhase.Act, kind = EventKind.Acted, target = "marta", actionId = "marta_sit",
                family = ActionFamily.Listen, witnesses = new[] { "pip" }, costAttention = 1,
            });
            var back = EventLog.FromJson(log.ToJson());
            Assert.AreEqual(1, back.Count);
            var e = back.Events[0];
            Assert.AreEqual("marta_sit", e.actionId);
            Assert.AreEqual(ActionFamily.Listen, e.family);
            CollectionAssert.AreEqual(new[] { "pip" }, e.witnesses);
        }

        [Test]
        public void RejectsUnknownSaveVersions()
        {
            Assert.Throws<FormatException>(() => EventLog.FromJson("{\"version\":99,\"events\":[]}"));
        }
    }

    public sealed class SliceDataTests
    {
        [Test]
        public void ShippedDataLoadsAndValidates()
        {
            var d = SliceData.LoadFromResources();
            CollectionAssert.AreEquivalent(new[] { "marta", "pip", "tobias" }, d.Villagers.Select(v => v.id));
        }

        [Test]
        public void EveryContextTheDirectorPlaysHasLines()
        {
            var d = SliceData.LoadFromResources();
            var needed = new[] { "intro", "hint:act", "hint:spent", "notice:already", "gather:intro", "gather:prompt", "gather:remind", "gather:quiet" }
                .Concat(d.Villagers.SelectMany(v => new[]
                {
                    $"notice:{v.id}", $"talk:{v.id}", $"gather:{v.id}:leaves",
                    $"gather:{v.id}:Close", $"gather:{v.id}:Edge", $"gather:{v.id}:Apart",
                }))
                .Concat(d.Actions.Select(a => (a.rebuffsFirstTime ? "rebuff:" : "act:") + a.id))
                .Concat(Enum.GetNames(typeof(OvenOutcome)).Select(o => $"morning:{o}"));

            var missing = needed.Where(c => !d.Dialogue.HasContext(c)).ToList();
            Assert.IsEmpty(missing, "Missing dialogue for: " + string.Join(", ", missing));
        }

        [Test]
        public void BadDataIsRejectedWithAllProblemsListed()
        {
            var ex = Assert.Throws<InvalidDataException>(() => SliceData.Parse(
                "{}",
                "{\"villagers\":[{\"id\":\"marta\"}]}",
                "{\"actions\":[{\"id\":\"x\",\"target\":\"nobody\",\"family\":\"Juggle\"}]}",
                "{\"signals\":[{\"id\":\"vibes\",\"weight\":1}],\"tiers\":[{\"name\":\"Guarded\",\"minWarmth\":0}]}",
                "{\"entries\":[]}"));
            StringAssert.Contains("unknown villager 'nobody'", ex.Message);
            StringAssert.Contains("unknown family 'Juggle'", ex.Message);
            StringAssert.Contains("'vibes'", ex.Message);
        }
    }
}
