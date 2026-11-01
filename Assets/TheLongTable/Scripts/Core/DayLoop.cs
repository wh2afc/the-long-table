using System;

namespace LongTable.Core
{
    /// <summary>
    /// Notice -> Act -> Gather -> (Feast at season's end) -> DayEnd.
    /// Also owns the daily attention budget.
    /// </summary>
    public sealed class DayLoop
    {
        private readonly int attentionPerDay;
        private readonly int seasonLength;

        public int Day { get; private set; } = 1;
        public DayPhase Phase { get; private set; } = DayPhase.Notice;
        public int AttentionLeft { get; private set; }
        public int AttentionPerDay => attentionPerDay;

        /// <summary>(from, to)</summary>
        public event Action<DayPhase, DayPhase> PhaseChanged;

        public DayLoop(int attentionPerDay, int seasonLength)
        {
            if (attentionPerDay < 0) throw new ArgumentOutOfRangeException(nameof(attentionPerDay));
            if (seasonLength < 1) throw new ArgumentOutOfRangeException(nameof(seasonLength));
            this.attentionPerDay = attentionPerDay;
            this.seasonLength = seasonLength;
            AttentionLeft = attentionPerDay;
        }

        public bool IsSeasonEnd => Day % seasonLength == 0;

        public DayPhase NextPhase => Phase switch
        {
            DayPhase.Notice => DayPhase.Act,
            DayPhase.Act => DayPhase.Gather,
            DayPhase.Gather => IsSeasonEnd ? DayPhase.Feast : DayPhase.DayEnd,
            DayPhase.Feast => DayPhase.DayEnd,
            _ => throw new InvalidOperationException("The day is over. Call StartNextDay()."),
        };

        public void Advance()
        {
            var from = Phase;
            Phase = NextPhase;
            PhaseChanged?.Invoke(from, Phase);
        }

        public bool CanSpend(int attention) => Phase == DayPhase.Act && attention <= AttentionLeft;

        public bool TrySpend(int attention)
        {
            if (attention < 0) throw new ArgumentOutOfRangeException(nameof(attention));
            if (!CanSpend(attention)) return false;
            AttentionLeft -= attention;
            return true;
        }

        public void StartNextDay()
        {
            if (Phase != DayPhase.DayEnd) throw new InvalidOperationException($"Can't start a new day during {Phase}.");
            var from = Phase;
            Day++;
            Phase = DayPhase.Notice;
            AttentionLeft = attentionPerDay;
            PhaseChanged?.Invoke(from, Phase);
        }
    }
}
