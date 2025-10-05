using UnityEngine;

public class ClientGameState : MonoBehaviour
{
    public static ClientGameState Instance { get; private set; }

    // ����� ����� ������� ������ ������ ����� ������
    public PlayerData CurrentPlayerData { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// ���������� �� DeviceAuthenticator ��� �������� ������.
    /// </summary>
    public void Initialize(PlayerData pd)
    {
        CurrentPlayerData = pd;
        // ����� �������������� �������������
    }
}