using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class CustomNetworkManager : NetworkManager
{
    [Header("Startup")]
    [SerializeField] private bool _autoStartServer = false;

    [SerializeField] private Button _connectButton;

    private void Awake()
    {
        if (_connectButton != null)
            _connectButton.onClick.AddListener(OnConnectClicked);
    }

    public override void Start()
    {
        base.Start();

        if (_autoStartServer && !NetworkServer.active)
            StartServer();
    }

    private void OnDestroy()
    {
        if (_connectButton != null)
            _connectButton.onClick.RemoveListener(OnConnectClicked);
    }

    private void OnConnectClicked() => StartClient();
}
