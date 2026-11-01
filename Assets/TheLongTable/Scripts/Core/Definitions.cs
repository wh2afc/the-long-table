using System;

namespace LongTable.Core
{
    // Authored data shapes. They are loaded from JSON in Resources/Data with
    // JsonUtility, so fields stay public and enums are written as strings.

    [Serializable]
    public sealed class SliceConfig
    {
        public int attentionPerDay = 4;
        public int startBread = 2;
        public int startCoin = 4;
        public int seasonLength = 7;
        public float witnessRadius = 14f;
        public float worldMinX = -18f;
        public float worldMaxX = 18f;
        public float playerStartX = 2f;

        // Warmth values that map to the muted and fully saturated village palette.
        public float mutedLookAtWarmth = 12f;
        public float fullLookAtWarmth = 50f;
    }

    [Serializable]
    public sealed class VillagerDef
    {
        public string id = "";
        public string displayName = "";
        public bool canRepay;
        public bool overlooked;

        public float homeX;
        public float seatCloseX;
        public float seatEdgeX;
        public float apartX;
        public bool hiddenWhenApart;

        public string bodyColor = "#888888";
        public string accentColor = "#555555";
        public float height = 1.7f;
        public float width = 0.7f;
    }

    [Serializable]
    public sealed class VillagerTable
    {
        public VillagerDef[] villagers = Array.Empty<VillagerDef>();
    }

    [Serializable]
    public sealed class ActionDef
    {
        public string id = "";
        public string target = "";
        public string label = "";
        public string family = "Give";
        public string visibility = "Private";

        public int costBread;
        public int costCoin;
        public int costAttention = 1;

        public bool rewardRequested;
        public string rewardItem = "";
        public float baseWarmth = 5f;

        public bool requiresNoticed;
        public bool rebuffsFirstTime;
        public bool requiresPriorRebuff;
        public bool hideAfterRebuff;

        [NonSerialized] public ActionFamily Family;
        [NonSerialized] public Visibility Visibility;
    }

    [Serializable]
    public sealed class ActionTable
    {
        public ActionDef[] actions = Array.Empty<ActionDef>();
    }
}
