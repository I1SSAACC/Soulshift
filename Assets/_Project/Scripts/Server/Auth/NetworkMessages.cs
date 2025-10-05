using Mirror;

public struct GetPlayerDataRequest : NetworkMessage
{
    public string PlayerGuid;
}

public struct GetPlayerDataResponse : NetworkMessage
{
    public string PlayerDataJson;
    public bool Success;
    public string ErrorMessage;
}