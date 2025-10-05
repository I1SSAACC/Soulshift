using System;
using System.Collections.Generic;

namespace PromoCodes
{
    [Serializable]
    public enum RewardType
    {
        Gold,
        Diamonds,
        Tokens
    }

    [Serializable]
    public class RewardOption
    {
        public RewardType type;
        public int amount;
    }

    [Serializable]
    public class PromoCodeEntry
    {
        public string code;
        public List<RewardOption> rewards = new List<RewardOption>();
    }

    [Serializable]
    public class PromoCodeList
    {
        public List<PromoCodeEntry> codes = new List<PromoCodeEntry>();
    }
}