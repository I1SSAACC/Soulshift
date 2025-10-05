using Mirror;
using PromoCodes;

public struct RedeemPromoRequest : NetworkMessage
{
    public string code;
}

public struct RedeemPromoResponse : NetworkMessage
{
    public bool success;
    public string message;
    public RewardType rewardType;
    public int amount;
}