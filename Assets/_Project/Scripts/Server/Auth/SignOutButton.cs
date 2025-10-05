using UnityEngine;
using UnityEngine.UI;
using Mirror;

[DisallowMultipleComponent]
public class SignOutButton : MonoBehaviour
{
    [SerializeField] private Button _button; // optional, можно не заполн€ть

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null)
            _button.onClick.AddListener(OnSignOutButtonPressed);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnSignOutButtonPressed);
    }

    public void OnSignOutButtonPressed()
    {
        // ”дал€ем только токен. ≈сли нужен полный сброс Ч заменить на DeleteAll.
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.AuthTokenKey);
        PlayerPrefs.Save();

        // ѕытаемс€ получить NetworkManager автоматически
        NetworkManager nm = NetworkManager.singleton;
        if (nm == null)
            nm = FindObjectOfType<NetworkManager>();

        // ќтключение / остановка
        if (nm != null)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                // хост (server+client) Ч корректно остановим хост
                nm.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                nm.StopClient();
            }
            else if (NetworkServer.active)
            {
                nm.StopServer();
            }
            else
            {
                // ничего не запущено, но на вс€кий случай попытатьс€ отключитьс€
                if (NetworkClient.isConnected) NetworkClient.Disconnect();
                if (NetworkServer.active) NetworkServer.Shutdown();
            }
        }
        else
        {
            // fallback: если NetworkManager не найден Ч попробовать напр€мую
            if (NetworkClient.isConnected) NetworkClient.Disconnect();
            if (NetworkServer.active) NetworkServer.Shutdown();
        }

        Debug.Log("[SignOut] Auth token cleared and network stopped.");
    }
}