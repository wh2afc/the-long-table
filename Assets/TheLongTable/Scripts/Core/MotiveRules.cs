using System;

namespace LongTable.Core
{
    /// <summary>
    /// Every number the motive evaluator uses. Loaded from Resources/Data/motive_rules.json
    /// so the design can be tuned without touching code.
    /// </summary>
    [Serializable]
    public sealed class MotiveRules
    {
        public float startWarmth = 15f;
        public float maxWarmth = 100f;

        // Warmth earned by an act is scaled between this and 1.0 by sincerity.
        public float minMultiplier = 0.2f;
        public float remindPenalty = 6f;
        public float neutralSincerity = 0.5f;

        // Share of acts that must be private / for people who can't repay
        // before those signals read as fully sincere.
        public float privateShareForFull = 0.5f;
        public float cannotRepayShareForFull = 0.34f;

        public SignalWeight[] signals = Array.Empty<SignalWeight>();
        public TierDef[] tiers = Array.Empty<TierDef>();
        public ActQuality act = new ActQuality();
        public OvenRules oven = new OvenRules();
        public SeatRules seats = new SeatRules();
    }

    [Serializable]
    public sealed class SignalWeight
    {
        public string id = "";
        public float weight = 1f;
    }

    [Serializable]
    public sealed class TierDef
    {
        public string name = "";
        public float minWarmth;
    }

    /// <summary>How one act's quality is judged when it feeds a villager's trust.</summary>
    [Serializable]
    public sealed class ActQuality
    {
        public float rewardRequestedPenalty = 0.6f;
        public float loudRescuePenalty = 0.5f;
        public float remindedPenalty = 0.4f;
        public float returnAfterRebuffBonus = 0.5f;
        public float rebuffedAttemptValue = 0.25f;
        public float maxQuality = 1.5f;
    }

    [Serializable]
    public sealed class OvenRules
    {
        public string owner = "marta";
        public string relitRequiresFamily = "Listen";
        public float relitMinTrust = 6f;
        public float relitMinSincerity = 0.6f;
    }

    [Serializable]
    public sealed class SeatRules
    {
        public float closeMinTrust = 6f;
        public float edgeMinTrust = 1f;
    }

    public static class SignalIds
    {
        public const string PrivateHelp = "private_help";
        public const string NoReminding = "no_reminding";
        public const string CannotRepay = "cannot_repay";
        public const string StaysThroughRebuff = "stays_through_rebuff";
        public const string NoRewardAsked = "no_reward_asked";
        public const string NoticedFirst = "noticed_first";

        public static readonly string[] All =
        {
            PrivateHelp, NoReminding, CannotRepay, StaysThroughRebuff, NoRewardAsked, NoticedFirst
        };
    }
}
