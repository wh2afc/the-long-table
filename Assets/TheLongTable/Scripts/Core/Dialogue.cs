using System;
using System.Collections.Generic;

namespace LongTable.Core
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string id = "";   // stable key for localization string tables
        public string who = "";  // villager id, "player", or "" for the narrator
        public string text = "";
    }

    /// <summary>
    /// A scripted beat. The entry with the highest priority whose conditions all
    /// hold is the one that plays for its context.
    /// </summary>
    [Serializable]
    public sealed class DialogueEntry
    {
        public string context = "";
        public int priority;

        public string tier = "";            // only when the village is in this warmth tier
        public string trustOf = "";         // villager id the trust bounds refer to
        public float minTrust = float.MinValue;
        public float maxTrust = float.MaxValue;
        public string requiresAction = "";  // only if this action was done (any day)
        public string forbidsAction = "";   // only if this action was never done

        public DialogueLine[] lines = Array.Empty<DialogueLine>();
    }

    [Serializable]
    public sealed class DialogueTable
    {
        public DialogueEntry[] entries = Array.Empty<DialogueEntry>();
    }

    /// <summary>What a dialogue condition may look at. Kept small on purpose.</summary>
    public interface IDialogueState
    {
        string Tier { get; }
        float TrustOf(string villagerId);
        bool HasDone(string actionId);
    }

    public sealed class DialogueDatabase
    {
        private readonly Dictionary<string, List<DialogueEntry>> byContext =
            new Dictionary<string, List<DialogueEntry>>(StringComparer.Ordinal);

        public DialogueDatabase(DialogueTable table)
        {
            foreach (var entry in table.entries)
            {
                if (!byContext.TryGetValue(entry.context, out var list))
                    byContext[entry.context] = list = new List<DialogueEntry>();
                list.Add(entry);
            }

            // Stable order: priority first, then authored order.
            foreach (var list in byContext.Values)
            {
                var indexed = new List<(DialogueEntry e, int i)>();
                for (int i = 0; i < list.Count; i++) indexed.Add((list[i], i));
                indexed.Sort((a, b) => a.e.priority != b.e.priority ? b.e.priority.CompareTo(a.e.priority) : a.i.CompareTo(b.i));
                list.Clear();
                foreach (var x in indexed) list.Add(x.e);
            }
        }

        public bool HasContext(string context) => byContext.ContainsKey(context);

        public IEnumerable<string> Contexts => byContext.Keys;

        /// <summary>Returns the lines for a context, or an empty array if nothing matches.</summary>
        public DialogueLine[] Pick(string context, IDialogueState state)
        {
            if (!byContext.TryGetValue(context, out var list)) return Array.Empty<DialogueLine>();
            foreach (var e in list)
            {
                if (Matches(e, state)) return e.lines;
            }
            return Array.Empty<DialogueLine>();
        }

        private static bool Matches(DialogueEntry e, IDialogueState s)
        {
            if (!string.IsNullOrEmpty(e.tier) && e.tier != s.Tier) return false;
            if (!string.IsNullOrEmpty(e.requiresAction) && !s.HasDone(e.requiresAction)) return false;
            if (!string.IsNullOrEmpty(e.forbidsAction) && s.HasDone(e.forbidsAction)) return false;
            if (!string.IsNullOrEmpty(e.trustOf))
            {
                float t = s.TrustOf(e.trustOf);
                if (t < e.minTrust || t > e.maxTrust) return false;
            }
            return true;
        }
    }
}
