using System;
using System.Collections.Generic;
using UnityEngine;

namespace LongTable.Core
{
    /// <summary>Append-only list of game events. Saved with the game as versioned JSON.</summary>
    public sealed class EventLog
    {
        public const int SaveVersion = 1;

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public List<GameEvent> events = new List<GameEvent>();
        }

        private readonly List<GameEvent> events = new List<GameEvent>();

        public event Action<GameEvent> Appended;

        public IReadOnlyList<GameEvent> Events => events;
        public int Count => events.Count;

        public GameEvent Append(GameEvent e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            e.seq = events.Count;
            if (e.witnesses == null) e.witnesses = Array.Empty<string>();
            events.Add(e);
            Appended?.Invoke(e);
            return e;
        }

        public string ToJson(bool pretty = false)
        {
            return JsonUtility.ToJson(new SaveData { version = SaveVersion, events = events }, pretty);
        }

        public static EventLog FromJson(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) throw new FormatException("Event log save is empty or not JSON.");
            if (data.version != SaveVersion)
                throw new FormatException($"Event log save version {data.version} is not supported (expected {SaveVersion}).");

            var log = new EventLog();
            foreach (var e in data.events) log.Append(e);
            return log;
        }
    }
}
