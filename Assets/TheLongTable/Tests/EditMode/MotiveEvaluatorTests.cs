using System.Collections.Generic;
using System.Linq;
using LongTable.Core;
using NUnit.Framework;

namespace LongTable.Tests
{
    /// <summary>
    /// The design depends on sincere and transactional play producing visibly
    /// different villages. These replay both reference patterns and check that
    /// the outputs differ the way the design document describes.
    /// </summary>
    public sealed class MotiveEvaluatorTests
    {
        private SliceData data;

        [SetUp]
        public void Load() => data = SliceData.LoadFromResources();

        /// <summary>Public acts are seen by every other villager; private acts by nobody.</summary>
        private IReadOnlyList<string> Everyone(ActionDef a) =>
            data.Villagers.Select(v => v.id).Where(id => id != a.target).ToList();

        private SliceSession Play(IEnumerable<PlayStep> steps)
        {
            var s = new SliceSession(data);
            ScriptedPlays.Run(s, steps, Everyone);
            return s;
        }

        [Test]
        public void BothReferencePlaysUseTheSameNumberOfActs()
        {
            var sincere = Play(ScriptedPlays.Sincere).Evaluate();
            var transactional = Play(ScriptedPlays.Transactional).Evaluate();
            Assert.AreEqual(sincere.Acts, transactional.Acts, "The comparison is only fair if both players were equally busy.");
            Assert.AreEqual(4, sincere.Acts);
        }

        [Test]
        public void SincerePlayReadsAsSincere()
        {
            var r = Play(ScriptedPlays.Sincere).Evaluate();
            Assert.That(r.Sincerity, Is.GreaterThan(0.85f), r.Describe());
            Assert.AreEqual("Open", r.Tier, r.Describe());
        }

        [Test]
        public void TransactionalPlayReadsAsTransactional()
        {
            var r = Play(ScriptedPlays.Transactional).Evaluate();
            Assert.That(r.Sincerity, Is.LessThan(0.4f), r.Describe());
            Assert.AreEqual("Guarded", r.Tier, r.Describe());
        }

        [Test]
        public void SincereVillageIsMuchWarmer()
        {
            var sincere = Play(ScriptedPlays.Sincere).Evaluate();
            var transactional = Play(ScriptedPlays.Transactional).Evaluate();
            Assert.That(sincere.Warmth - transactional.Warmth, Is.GreaterThan(15f),
                $"Sincere:\n{sincere.Describe()}\nTransactional:\n{transactional.Describe()}");
        }

        [Test]
        public void MartasOvenRelightsOnlyForTheSincereDay()
        {
            Assert.AreEqual(OvenOutcome.Relit, Play(ScriptedPlays.Sincere).Evaluate().Oven);
            Assert.AreEqual(OvenOutcome.Polite, Play(ScriptedPlays.Transactional).Evaluate().Oven);
            Assert.AreEqual(OvenOutcome.Cold, Play(new[] { new PlayStep(PlayStepKind.Advance), new PlayStep(PlayStepKind.Advance) }).Evaluate().Oven);
        }

        [Test]
        public void FixingTheFlueIsNotTheSameAsSittingWithHer()
        {
            // Help that fixes things but never listens keeps the oven merely polite.
            var r = Play(new[]
            {
                new PlayStep(PlayStepKind.Notice, "marta"), new PlayStep(PlayStepKind.Advance),
                new PlayStep(PlayStepKind.Act, "marta_fix_flue"),
            }).Evaluate();
            Assert.AreEqual(OvenOutcome.Polite, r.Oven, r.Describe());
        }

        [Test]
        public void TheTableLooksDifferent()
        {
            var sincere = Play(ScriptedPlays.Sincere).Evaluate();
            var transactional = Play(ScriptedPlays.Transactional).Evaluate();
            foreach (var v in data.Villagers)
                Assert.AreEqual(Seat.Close, sincere.SeatOf(v.id), $"{v.id} should sit close after a sincere day.\n{sincere.Describe()}");
            Assert.AreEqual(Seat.Apart, transactional.SeatOf("pip"), "A loud rescue costs Pip his pride.");
            Assert.AreEqual(Seat.Apart, transactional.SeatOf("tobias"), "Buying from Tobias is not the same as being there.");
        }

        [Test]
        public void TransactionalRewardsAreReceivedButHollow()
        {
            var r = Play(ScriptedPlays.Transactional).Evaluate();
            CollectionAssert.AreEquivalent(new[] { "loaves", "hoe" }, r.RewardsReceived);
            Assert.That(r.TrustOf("tobias"), Is.LessThan(data.Rules.seats.edgeMinTrust));
        }

        [Test]
        public void ReturningAfterARebuffBuildsTrust()
        {
            var gaveUp = Play(new[]
            {
                new PlayStep(PlayStepKind.Notice, "tobias"), new PlayStep(PlayStepKind.Advance),
                new PlayStep(PlayStepKind.Act, "tobias_offer_help"),
            }).Evaluate();
            var stayed = Play(new[]
            {
                new PlayStep(PlayStepKind.Notice, "tobias"), new PlayStep(PlayStepKind.Advance),
                new PlayStep(PlayStepKind.Act, "tobias_offer_help"), new PlayStep(PlayStepKind.Act, "tobias_return"),
            }).Evaluate();

            Assert.That(stayed.TrustOf("tobias"), Is.GreaterThan(gaveUp.TrustOf("tobias") + 5f));
            var signal = stayed.Signals.Single(s => s.Id == SignalIds.StaysThroughRebuff);
            Assert.AreEqual(1f, signal.Score);
        }

        [Test]
        public void HelpingOnlyWhenWatchedLowersSincerity()
        {
            var quiet = Play(new[]
            {
                new PlayStep(PlayStepKind.Notice, "pip"), new PlayStep(PlayStepKind.Advance),
                new PlayStep(PlayStepKind.Act, "pip_bread_quiet"),
            }).Evaluate();
            var loud = Play(new[]
            {
                new PlayStep(PlayStepKind.Notice, "pip"), new PlayStep(PlayStepKind.Advance),
                new PlayStep(PlayStepKind.Act, "pip_bread_public"),
            }).Evaluate();

            Assert.That(quiet.Sincerity, Is.GreaterThan(loud.Sincerity));
            Assert.That(quiet.TrustOf("pip"), Is.GreaterThan(loud.TrustOf("pip")));
        }

        [Test]
        public void RemindingPeopleCostsWarmth()
        {
            var steps = new List<PlayStep>
            {
                new PlayStep(PlayStepKind.Notice, "pip"), new PlayStep(PlayStepKind.Advance),
                new PlayStep(PlayStepKind.Act, "pip_bread_quiet"), new PlayStep(PlayStepKind.Advance),
            };
            var silent = Play(steps).Evaluate();
            var boasted = Play(steps.Append(new PlayStep(PlayStepKind.Remind))).Evaluate();

            Assert.That(boasted.Warmth, Is.LessThan(silent.Warmth));
            Assert.That(boasted.Sincerity, Is.LessThan(silent.Sincerity));
        }

        [Test]
        public void EmptyDayIsNeutralAndStaysAtStartingWarmth()
        {
            var r = Play(new PlayStep[0]).Evaluate();
            Assert.AreEqual(data.Rules.neutralSincerity, r.Sincerity);
            Assert.AreEqual(data.Rules.startWarmth, r.Warmth);
            Assert.That(r.Signals.All(s => s.Evidence == 0 || s.Id == SignalIds.StaysThroughRebuff));
        }

        [Test]
        public void EvaluationIsDeterministic()
        {
            var a = Play(ScriptedPlays.Sincere).Evaluate().Describe();
            var b = Play(ScriptedPlays.Sincere).Evaluate().Describe();
            Assert.AreEqual(a, b);
        }

        [Test]
        public void EvaluatingASavedLogGivesTheSameAnswer()
        {
            var session = Play(ScriptedPlays.Transactional);
            var restored = EventLog.FromJson(session.Log.ToJson());
            Assert.AreEqual(session.Evaluate().Describe(), MotiveEvaluator.Evaluate(restored, data).Describe());
        }
    }
}
