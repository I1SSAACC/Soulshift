using Mirror;
using UnityEngine;

public class ClientGameState : MonoBehaviour
{
    public static ClientGameState Instance { get; private set; }

    public PlayerData CurrentPlayerData { get; private set; }
    public string PlayerGuid { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        NetworkClient.RegisterHandler<GetPlayerDataResponse>(OnGetPlayerDataResponse, false);
    }

    private void OnDisable()
    {
        if (NetworkClient.active)
            NetworkClient.UnregisterHandler<GetPlayerDataResponse>();
    }

    // ���������� ����� �������� �������������� � �������� ���� guid, ������� ������ ������
    public void SetPlayerGuid(string guid)
    {
        PlayerGuid = guid;
    }

    // ������ ������ ������ � �������
    public void RequestPlayerData()
    {
        if (string.IsNullOrEmpty(PlayerGuid))
        {
            Debug.LogWarning("[ClientGameState] PlayerGuid is empty, cannot request data.");
            return;
        }

        if (NetworkClient.isConnected == false)
        {
            Debug.LogWarning("[ClientGameState] Not connected to server.");
            return;
        }

        GetPlayerDataRequest req = new GetPlayerDataRequest { PlayerGuid = PlayerGuid };
        NetworkClient.Send(req);
    }

    private void OnGetPlayerDataResponse(GetPlayerDataResponse msg)
    {
        if (msg.Success == false)
        {
            Debug.LogError($"[ClientGameState] Failed to get player data: {msg.ErrorMessage}");
            return;
        }

        if (string.IsNullOrEmpty(msg.PlayerDataJson))
        {
            Debug.LogWarning("[ClientGameState] Empty player data json received.");
            return;
        }

        try
        {
            CurrentPlayerData = Utils.FromJson<PlayerData>(msg.PlayerDataJson);
            Debug.Log("[ClientGameState] Player data updated from server.");
            // ���������� ���� ���������� � ������� �������: �������� ������� ����� Unity messaging
            // ��� ����� ����� ������ UI (��������, PlayerStatsDisplay ����� �����������)
            // ��� ��������: ����� ��� PlayerStatsDisplay � ����� � ������� ��
            var displays = FindObjectsOfType<PlayerStatsDisplay>();
            foreach (var d in displays)
                d.UpdateStats();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ClientGameState] Failed to parse player data json: {ex}");
        }
    }
}