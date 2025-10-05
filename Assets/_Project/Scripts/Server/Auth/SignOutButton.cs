using UnityEngine;
using UnityEngine.UI;
using Mirror;

[DisallowMultipleComponent]
public class SignOutButton : MonoBehaviour
{
    [SerializeField] private Button _button; // optional, ����� �� ���������

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
        // ������� ������ �����. ���� ����� ������ ����� � �������� �� DeleteAll.
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.AuthTokenKey);
        PlayerPrefs.Save();

        // �������� �������� NetworkManager �������������
        NetworkManager nm = NetworkManager.singleton;
        if (nm == null)
            nm = FindObjectOfType<NetworkManager>();

        // ���������� / ���������
        if (nm != null)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                // ���� (server+client) � ��������� ��������� ����
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
                // ������ �� ��������, �� �� ������ ������ ���������� �����������
                if (NetworkClient.isConnected) NetworkClient.Disconnect();
                if (NetworkServer.active) NetworkServer.Shutdown();
            }
        }
        else
        {
            // fallback: ���� NetworkManager �� ������ � ����������� ��������
            if (NetworkClient.isConnected) NetworkClient.Disconnect();
            if (NetworkServer.active) NetworkServer.Shutdown();
        }

        Debug.Log("[SignOut] Auth token cleared and network stopped.");
    }
}