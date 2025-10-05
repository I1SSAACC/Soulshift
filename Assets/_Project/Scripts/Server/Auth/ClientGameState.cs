using UnityEngine;

public class ClientGameState : MonoBehaviour
{
    public static ClientGameState Instance { get; private set; }

    // «десь будем хранить данные игрока после логина
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
    /// ¬ызываетс€ из DeviceAuthenticator при успешном логине.
    /// </summary>
    public void Initialize(PlayerData pd)
    {
        CurrentPlayerData = pd;
        // люба€ дополнительна€ инициализаци€
    }
}