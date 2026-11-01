using System;
using System.Collections.Generic;

namespace LongTable.Core
{
    public enum PlayStepKind { Notice, Act, Advance, Remind }

    public readonly struct PlayStep
    {
        public readonly PlayStepKind Kind;
        public readonly string Id;

        public PlayStep(PlayStepKind kind, string id = "") { Kind = kind; Id = id; }

        public override string ToString() => string.IsNullOrEmpty(Id) ? Kind.ToString() : $"{Kind} {Id}";
    }

    /// <summary>
    /// Two reference ways to play the same day, used by the automated tests and
    /// the in-game autoplay. Both do four acts with the same attention budget;
    /// only the why differs.
    /// </summary>
    public static class ScriptedPlays
    {
        private static PlayStep Notice(string id) => new PlayStep(PlayStepKind.Notice, id);
        private static PlayStep Act(string id) => new PlayStep(PlayStepKind.Act, id);
        private static readonly PlayStep Advance = new PlayStep(PlayStepKind.Advance);
        private static readonly PlayStep Remind = new PlayStep(PlayStepKind.Remind);

        /// <summary>Notices first, helps quietly, sits with Marta, goes back to Tobias after he snaps.</summary>
        public static readonly PlayStep[] Sincere =
        {
            Notice("pip"), Notice("marta"), Notice("tobias"),
            Advance,
            Act("pip_bread_quiet"), Act("marta_sit"), Act("tobias_offer_help"), Act("tobias_return"),
            Advance,
            Advance,
        };

        /// <summary>Skips noticing, helps in public, pays for favours, then tells the table about it.</summary>
        public static readonly PlayStep[] Transactional =
        {
            Advance,
            Act("pip_bread_public"), Act("marta_commission"), Act("tobias_trade"), Act("marta_fix_flue"),
            Advance,
            Remind,
            Advance,
        };

        public static PlayStep[] ByName(string name) => name?.ToLowerInvariant() switch
        {
            "sincere" => Sincere,
            "transactional" => Transactional,
            _ => throw new ArgumentException($"No scripted play named '{name}'. Use sincere or transactional."),
        };

        /// <summary>Applies one step. Throws if the step is not allowed, so a broken script fails loudly.</summary>
        public static void Apply(SliceSession s, PlayStep step, Func<ActionDef, IReadOnlyList<string>> witnesses)
        {
            switch (step.Kind)
            {
                case PlayStepKind.Notice:
                    if (!s.Notice(step.Id)) throw new InvalidOperationException($"Step '{step}' was refused.");
                    break;
                case PlayStepKind.Act:
                    var def = s.Data.Action(step.Id);
                    var r = s.Perform(step.Id, witnesses(def));
                    if (!r.Performed) throw new InvalidOperationException($"Step '{step}' was refused: {r.FailReason}");
                    break;
                case PlayStepKind.Advance:
                    s.AdvancePhase();
                    break;
                case PlayStepKind.Remind:
                    if (!s.Remind()) throw new InvalidOperationException($"Step '{step}' was refused.");
                    break;
            }
        }

        public static void Run(SliceSession s, IEnumerable<PlayStep> steps, Func<ActionDef, IReadOnlyList<string>> witnesses)
        {
            foreach (var step in steps) Apply(s, step, witnesses);
        }
    }
}
