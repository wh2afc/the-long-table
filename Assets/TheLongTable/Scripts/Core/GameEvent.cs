using System;

namespace LongTable.Core
{
    public enum DayPhase { Notice, Act, Gather, Feast, DayEnd }

    public enum EventKind { Noticed, Acted, Rebuffed, Reminded, PhaseChanged }

    public enum ActionFamily { Give, Mend, Build, Listen }

    public enum Visibility { Private, Public }

    /// <summary>
    /// One small, self-contained record of something that happened: who did what,
    /// to whom, what it cost, who saw it, and when. The motive evaluator reads
    /// only these records, never live game objects.
    /// </summary>
    [Serializable]
    public sealed class GameEvent
    {
        public int seq;
        public int day;
        public DayPhase phase;
        public EventKind kind;

        public string actor = "player";
        public string target = "";
        public string actionId = "";
        public ActionFamily family;
        public Visibility visibility;
        public string[] witnesses = Array.Empty<string>();

        public int costBread;
        public int costCoin;
        public int costAttention;

        public bool rewardRequested;
        public string rewardItem = "";

        // Copied from the villager definition at the moment of the act, so old
        // saves keep their meaning even if the data changes later.
        public bool targetCanRepay;
        public bool targetOverlooked;

        public bool wasRebuffed;
        public string note = "";

        public bool Witnessed => witnesses != null && witnesses.Length > 0;
        public bool IsPlayerAct => kind == EventKind.Acted && actor == "player";
    }
}
