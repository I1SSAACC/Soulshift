using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class LogoutController : MonoBehaviour
{
    [SerializeField] private Button _logoutButton;
    [SerializeField] private float _waitBeforeStopSeconds = 0.35f;

    private void Start()
    {
        if (_logoutButton != null)
            _logoutButton.onClick.AddListener(OnLogoutClicked);
    }

    private void OnDestroy()
    {
        if (_logoutButton != null)
            _logoutButton.onClick.RemoveListener(OnLogoutClicked);
    }

    private void OnLogoutClicked()
    {
        AuthRequestData.Type = AuthType.None;

        if (NetworkClient.isConnected)
            NetworkClient.Send(new LogoutRequestMessage { deviceId = DeviceIdHelper.GetLocalDeviceId() });

        StartCoroutine(WaitAndStopClient());
    }

    private IEnumerator WaitAndStopClient()
    {
        yield return new WaitForSeconds(_waitBeforeStopSeconds);

        if (NetworkManager.singleton != null && NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        if (AuthUIController.Instance != null)
            AuthUIController.Instance.HandleLoggedOut();
    }
}