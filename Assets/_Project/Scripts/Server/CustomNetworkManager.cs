using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    [Header("Startup")]
    [SerializeField] private bool _autoStartServer = false;

    private readonly Dictionary<NetworkConnectionToClient, string> _connectionToGuid = new Dictionary<NetworkConnectionToClient, string>();
    private readonly object _connLock = new object();
    private bool _isConnectingClient;

    // Allows server to map connection -> guid
    public void AddConnectionGuid(NetworkConnectionToClient conn, string guid)
    {
        if (conn == null || string.IsNullOrEmpty(guid)) return;
        lock (_connLock) _connectionToGuid[conn] = guid;
    }

    public bool TryGetGuid(NetworkConnectionToClient conn, out string guid)
    {
        lock (_connLock) return _connectionToGuid.TryGetValue(conn, out guid);
    }

    public void RemoveConnectionGuid(NetworkConnectionToClient conn)
    {
        if (conn == null) return;
        lock (_connLock)
        {
            if (_connectionToGuid.ContainsKey(conn))
                _connectionToGuid.Remove(conn);
        }
    }

    public override void Start()
    {
        base.Start();

        if (_autoStartServer && !NetworkServer.active)
            StartServer();
        else
            StartClient();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // nothing special here, ensure DeviceAuthenticator component is present on manager object
        if (GetComponent<DeviceAuthenticator>() == null)
            Debug.LogWarning("[CustomNetworkManager] DeviceAuthenticator not found on NetworkManager GameObject.");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        string guid = null;
        lock (_connLock)
        {
            if (_connectionToGuid.TryGetValue(conn, out guid))
                _connectionToGuid.Remove(conn);
        }

        if (!string.IsNullOrEmpty(guid))
        {
            ServerDataManager dm = ServerDataManager.Instance;
            if (dm != null)
            {
                dm.MarkAccountOffline(guid);
                Debug.Log($"[CustomNetworkManager] Marked account offline for guid={guid} on disconnect");
            }
        }

        base.OnServerDisconnect(conn);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[CustomNetworkManager] OnClientConnect");

        // Send AutoLoginRequest only if the client explicitly requested Auto auth
        if (AuthRequestData.Type == AuthType.Auto)
        {
            string localDeviceId = DeviceIdHelper.GetLocalDeviceId();
            try
            {
                NetworkClient.Send(new AutoLoginRequestMessage { deviceId = localDeviceId });
                Debug.Log($"[CustomNetworkManager] Sent AutoLoginRequest deviceId={localDeviceId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CustomNetworkManager] Failed to send AutoLoginRequest: {ex}");
            }
        }

        _isConnectingClient = false;
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        _isConnectingClient = false;
        Debug.Log("[CustomNetworkManager] Client disconnected.");
    }
}