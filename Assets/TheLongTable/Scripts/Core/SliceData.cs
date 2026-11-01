using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LongTable.Core
{
    /// <summary>All authored data for the slice, loaded and checked in one place.</summary>
    public sealed class SliceData
    {
        public const string ResourceFolder = "Data/";

        public SliceConfig Config { get; private set; }
        public MotiveRules Rules { get; private set; }
        public IReadOnlyList<VillagerDef> Villagers { get; private set; }
        public IReadOnlyList<ActionDef> Actions { get; private set; }
        public DialogueTable DialogueTable { get; private set; }
        public DialogueDatabase Dialogue { get; private set; }

        private readonly Dictionary<string, VillagerDef> villagerById = new Dictionary<string, VillagerDef>();
        private readonly Dictionary<string, ActionDef> actionById = new Dictionary<string, ActionDef>();

        public VillagerDef Villager(string id) =>
            villagerById.TryGetValue(id, out var v) ? v : throw new KeyNotFoundException($"No villager '{id}'.");

        public bool TryVillager(string id, out VillagerDef v) => villagerById.TryGetValue(id ?? "", out v);

        public ActionDef Action(string id) =>
            actionById.TryGetValue(id, out var a) ? a : throw new KeyNotFoundException($"No action '{id}'.");

        public bool TryAction(string id, out ActionDef a) => actionById.TryGetValue(id ?? "", out a);

        public static SliceData LoadFromResources()
        {
            string Load(string name)
            {
                var asset = Resources.Load<TextAsset>(ResourceFolder + name);
                if (asset == null) throw new FileNotFoundException($"Missing Resources/{ResourceFolder}{name}.json");
                return asset.text;
            }

            return Parse(Load("slice_config"), Load("villagers"), Load("actions"), Load("motive_rules"), Load("dialogue"));
        }

        public static SliceData Parse(string configJson, string villagersJson, string actionsJson, string rulesJson, string dialogueJson)
        {
            var data = new SliceData
            {
                Config = JsonUtility.FromJson<SliceConfig>(configJson),
                Rules = JsonUtility.FromJson<MotiveRules>(rulesJson),
                DialogueTable = JsonUtility.FromJson<DialogueTable>(dialogueJson),
            };
            data.Villagers = JsonUtility.FromJson<VillagerTable>(villagersJson).villagers;
            data.Actions = JsonUtility.FromJson<ActionTable>(actionsJson).actions;

            var errors = data.Index();
            if (errors.Count > 0)
                throw new InvalidDataException("Slice data has problems:\n- " + string.Join("\n- ", errors));

            data.Dialogue = new DialogueDatabase(data.DialogueTable);
            return data;
        }

        private List<string> Index()
        {
            var errors = new List<string>();

            foreach (var v in Villagers)
            {
                if (string.IsNullOrEmpty(v.id)) { errors.Add("A villager has no id."); continue; }
                if (!villagerById.TryAdd(v.id, v)) errors.Add($"Villager id '{v.id}' is used twice.");
            }

            foreach (var a in Actions)
            {
                if (string.IsNullOrEmpty(a.id)) { errors.Add("An action has no id."); continue; }
                if (!actionById.TryAdd(a.id, a)) errors.Add($"Action id '{a.id}' is used twice.");
                if (!villagerById.ContainsKey(a.target)) errors.Add($"Action '{a.id}' targets unknown villager '{a.target}'.");
                if (Enum.TryParse(a.family, out ActionFamily fam)) a.Family = fam;
                else errors.Add($"Action '{a.id}' has unknown family '{a.family}' (Give, Mend, Build, Listen).");
                if (Enum.TryParse(a.visibility, out Visibility vis)) a.Visibility = vis;
                else errors.Add($"Action '{a.id}' has unknown visibility '{a.visibility}' (Private, Public).");
                if (a.costAttention < 0 || a.costBread < 0 || a.costCoin < 0) errors.Add($"Action '{a.id}' has a negative cost.");
                if (a.requiresPriorRebuff && a.rebuffsFirstTime) errors.Add($"Action '{a.id}' cannot both rebuff and require a rebuff.");
            }

            var known = new HashSet<string>(SignalIds.All);
            foreach (var s in Rules.signals)
            {
                if (!known.Contains(s.id)) errors.Add($"Motive rule signal '{s.id}' is not one the evaluator knows.");
                if (s.weight < 0) errors.Add($"Motive rule signal '{s.id}' has a negative weight.");
            }
            if (Rules.tiers.Length == 0) errors.Add("Motive rules define no warmth tiers.");
            if (!villagerById.ContainsKey(Rules.oven.owner)) errors.Add($"Oven owner '{Rules.oven.owner}' is not a villager.");
            if (!Enum.TryParse(Rules.oven.relitRequiresFamily, out ActionFamily _))
                errors.Add($"Oven rule family '{Rules.oven.relitRequiresFamily}' is unknown.");

            var tierNames = new HashSet<string>();
            foreach (var t in Rules.tiers) tierNames.Add(t.name);

            var lineIds = new HashSet<string>();
            foreach (var e in DialogueTable.entries)
            {
                if (string.IsNullOrEmpty(e.context)) errors.Add("A dialogue entry has no context.");
                if (!string.IsNullOrEmpty(e.tier) && !tierNames.Contains(e.tier))
                    errors.Add($"Dialogue '{e.context}' uses unknown tier '{e.tier}'.");
                if (!string.IsNullOrEmpty(e.trustOf) && !villagerById.ContainsKey(e.trustOf))
                    errors.Add($"Dialogue '{e.context}' checks trust of unknown villager '{e.trustOf}'.");
                foreach (var act in new[] { e.requiresAction, e.forbidsAction })
                    if (!string.IsNullOrEmpty(act) && !actionById.ContainsKey(act))
                        errors.Add($"Dialogue '{e.context}' refers to unknown action '{act}'.");
                foreach (var line in e.lines)
                {
                    if (string.IsNullOrEmpty(line.id)) errors.Add($"A line in '{e.context}' has no id.");
                    else if (!lineIds.Add(line.id)) errors.Add($"Dialogue line id '{line.id}' is used twice.");
                    if (!string.IsNullOrEmpty(line.who) && line.who != "player" && !villagerById.ContainsKey(line.who))
                        errors.Add($"Line '{line.id}' is spoken by unknown '{line.who}'.");
                }
            }

            return errors;
        }
    }
}
