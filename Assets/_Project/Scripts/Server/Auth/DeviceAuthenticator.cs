using System;
using Mirror;
using UnityEngine;

#region Messages
public struct RegisterRequestMessage : NetworkMessage
{
    public string email;
    public string nickname;
    public string passwordHash;
    public string deviceId;
}

public struct RegisterResponseMessage : NetworkMessage
{
    public bool success;
    public string message;
}

public struct LoginRequestMessage : NetworkMessage
{
    public string nickname;
    public string passwordHash;
    public string deviceId;
    public bool rememberMe;
}

public struct LoginResponseMessage : NetworkMessage
{
    public bool success;
    public string message;
    public string playerJson;
}

public struct AutoLoginRequestMessage : NetworkMessage
{
    public string deviceId;
}

public struct LogoutRequestMessage : NetworkMessage
{
    public string deviceId;
}
#endregion

public enum AuthType { None, Login, Auto }

public static class AuthRequestData
{
    public static AuthType Type;
    public static string Nickname;
    public static string Password;
    public static bool RememberMe;
}

public class DeviceAuthenticator : NetworkAuthenticator
{
    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<RegisterRequestMessage>(OnRegisterRequest, false);
        NetworkServer.RegisterHandler<LoginRequestMessage>(OnLoginRequest, false);
        NetworkServer.RegisterHandler<AutoLoginRequestMessage>(OnAutoLoginRequest, false);
        NetworkServer.RegisterHandler<LogoutRequestMessage>(OnLogoutRequest, false);
    }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn) { }

    private void OnRegisterRequest(NetworkConnectionToClient conn, RegisterRequestMessage msg)
    {
        if (string.IsNullOrEmpty(msg.email) || string.IsNullOrEmpty(msg.nickname) || string.IsNullOrEmpty(msg.passwordHash))
        {
            conn.Send(new RegisterResponseMessage { success = false, message = "Invalid registration data" });
            return;
        }

        ServerDataManager dm = ServerDataManager.Instance;
        if (dm == null)
        {
            conn.Send(new RegisterResponseMessage { success = false, message = "Server data unavailable" });
            Debug.LogError("[AuthServer] ServerDataManager.Instance is null on register");
            return;
        }

        if (dm.CreateAccount(msg.email, msg.nickname, msg.passwordHash, out string createMessage) == false)
        {
            conn.Send(new RegisterResponseMessage { success = false, message = createMessage });
            return;
        }

        AccountEntry createdEntry = dm.accountsDb.Accounts.Find(a => string.Equals(a.Nickname, msg.nickname, StringComparison.OrdinalIgnoreCase));
        if (createdEntry == null)
        {
            conn.Send(new RegisterResponseMessage { success = false, message = "Registration failed (no entry)" });
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(msg.deviceId))
            {
                PlayerData pd = dm.LoadOrCreatePlayer(createdEntry.Guid);
                if (pd.DeviceId == null) pd.DeviceId = new System.Collections.Generic.List<string>();
                if (!pd.DeviceId.Contains(msg.deviceId))
                {
                    pd.DeviceId.Add(msg.deviceId);
                    dm.SavePlayer(pd);
                    Debug.Log($"[AuthServer] Attached device {msg.deviceId} to player {createdEntry.Guid}");
                }
                else
                {
                    Debug.Log($"[AuthServer] Device {msg.deviceId} already present for player {createdEntry.Guid}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AuthServer] Failed to attach device id on registration: {ex}");
            conn.Send(new RegisterResponseMessage { success = true, message = "Registered but device attach failed" });
            return;
        }

        // Reply register success
        conn.Send(new RegisterResponseMessage { success = true, message = "Registered successfully" });
        Debug.Log($"[AuthServer] Registered new account {msg.nickname}");

        // Attempt auto-login: mark online, map connection, send LoginResponse, ServerAccept
        try
        {
            dm.MarkAccountOnline(createdEntry.Guid);

            if (NetworkManager.singleton is CustomNetworkManager nm)
                nm.AddConnectionGuid(conn, createdEntry.Guid);

            PlayerData pdToSend = dm.LoadOrCreatePlayer(createdEntry.Guid);
            string playerJson = JsonUtility.ToJson(pdToSend, true);

            conn.Send(new LoginResponseMessage { success = true, playerJson = playerJson, message = "Login after registration successful" });

            ServerAccept(conn);

            Debug.Log($"[AuthServer] Auto-logged in newly registered account {createdEntry.Nickname} from connection {conn.connectionId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthServer] Failed to finalize login after registration: {ex}");
            dm.MarkAccountOffline(createdEntry.Guid);
            conn.Send(new LoginResponseMessage { success = false, message = "Registration succeeded but auto-login failed" });
        }
    }

    private void OnLoginRequest(NetworkConnectionToClient conn, LoginRequestMessage msg)
    {
        if (string.IsNullOrEmpty(msg.nickname) || string.IsNullOrEmpty(msg.passwordHash))
        {
            conn.Send(new LoginResponseMessage { success = false, message = "Invalid credentials" });
            return;
        }

        ServerDataManager dm = ServerDataManager.Instance;
        if (dm == null)
        {
            conn.Send(new LoginResponseMessage { success = false, message = "Server data unavailable" });
            Debug.LogError("[AuthServer] ServerDataManager.Instance is null on login");
            return;
        }

        AccountEntry entry = dm.VerifyLogin(msg.nickname, msg.passwordHash, msg.rememberMe, msg.deviceId, out string verifyMessage);
        if (entry == null)
        {
            conn.Send(new LoginResponseMessage { success = false, message = verifyMessage });
            return;
        }

        // If account is reported online by VerifyLogin (it sets IsOnline) we proceed normally.
        // Add mapping and send player data
        if (NetworkManager.singleton is CustomNetworkManager nmAdd)
            nmAdd.AddConnectionGuid(conn, entry.Guid);

        PlayerData pd;
        try
        {
            pd = dm.LoadOrCreatePlayer(entry.Guid);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthServer] Failed to load player data for {entry.Guid}: {ex}");
            conn.Send(new LoginResponseMessage { success = false, message = "Failed to load player data" });
            return;
        }

        conn.Send(new LoginResponseMessage { success = true, playerJson = JsonUtility.ToJson(pd, true), message = "Login successful" });

        ServerAccept(conn);
        Debug.Log($"[AuthServer] Account {entry.Nickname} logged in from connection {conn.connectionId}");
    }

    private void OnAutoLoginRequest(NetworkConnectionToClient conn, AutoLoginRequestMessage msg)
    {
        Debug.Log($"[AuthServer] OnAutoLoginRequest received deviceId={msg.deviceId} from connection {conn.connectionId}");

        if (string.IsNullOrEmpty(msg.deviceId))
        {
            conn.Send(new LoginResponseMessage { success = false, message = "Invalid device id" });
            return;
        }

        ServerDataManager dm = ServerDataManager.Instance;
        if (dm == null)
        {
            conn.Send(new LoginResponseMessage { success = false, message = "Server data unavailable" });
            Debug.LogError("[AuthServer] ServerDataManager.Instance is null on autologin");
            return;
        }

        AccountEntry found = dm.FindByDeviceId(msg.deviceId);

        if (found == null)
        {
            conn.Send(new LoginResponseMessage { success = false, message = "No account for this device" });
            Debug.Log($"[AuthServer] FindByDeviceId returned null for deviceId={msg.deviceId}");
            return;
        }

        // Tolerant handling: if account already marked online, allow reattach if same connection already mapped
        if (found.IsOnline)
        {
            if (NetworkManager.singleton is CustomNetworkManager nm)
            {
                if (nm.TryGetGuid(conn, out string existingGuid) && string.Equals(existingGuid, found.Guid, StringComparison.Ordinal))
                {
                    Debug.Log($"[AuthServer] Re-attaching existing connection for guid {found.Guid}");
                    // allow proceed
                }
                else
                {
                    conn.Send(new LoginResponseMessage { success = false, message = "Account already online" });
                    Debug.Log($"[AuthServer] Auto-login rejected because account {found.Nickname} is already online");
                    return;
                }
            }
            else
            {
                conn.Send(new LoginResponseMessage { success = false, message = "Account already online" });
                Debug.Log($"[AuthServer] Auto-login rejected because account {found.Nickname} is already online (no nm)");
                return;
            }
        }

        dm.MarkAccountOnline(found.Guid);

        if (NetworkManager.singleton is CustomNetworkManager nmAdd)
            nmAdd.AddConnectionGuid(conn, found.Guid);

        PlayerData pdFound;
        try
        {
            pdFound = dm.LoadOrCreatePlayer(found.Guid);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthServer] Failed to load player data for {found.Guid}: {ex}");
            conn.Send(new LoginResponseMessage { success = false, message = "Failed to load player data" });
            return;
        }

        conn.Send(new LoginResponseMessage { success = true, playerJson = JsonUtility.ToJson(pdFound, true), message = "Auto-login successful" });
        ServerAccept(conn);
        Debug.Log($"[AuthServer] Auto-logged in {found.Nickname} from device {msg.deviceId}");
    }

    private void OnLogoutRequest(NetworkConnectionToClient conn, LogoutRequestMessage msg)
    {
        ServerDataManager dm = ServerDataManager.Instance;
        if (dm == null)
        {
            Debug.LogWarning("[AuthServer] ServerDataManager.Instance is null on logout");
            return;
        }

        if (NetworkManager.singleton is CustomNetworkManager nm)
        {
            if (nm.TryGetGuid(conn, out string guid))
            {
                if (!string.IsNullOrEmpty(guid))
                {
                    if (!string.IsNullOrEmpty(msg.deviceId))
                        dm.RemoveDeviceIdFromPlayer(guid, msg.deviceId);

                    dm.MarkAccountOffline(guid);
                }

                nm.RemoveConnectionGuid(conn);
            }
        }

        conn.Disconnect();
        Debug.Log($"[AuthServer] Logout processed for connection {conn.connectionId}");
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<LoginResponseMessage>(OnClientLoginResponse, false);
    }

    public override void OnClientAuthenticate()
    {
        switch (AuthRequestData.Type)
        {
            case AuthType.Login:
                if (string.IsNullOrEmpty(AuthRequestData.Nickname) || string.IsNullOrEmpty(AuthRequestData.Password))
                {
                    Debug.LogWarning("[AuthClient] No credentials provided");
                    return;
                }

                LoginRequestMessage loginMsg = new LoginRequestMessage
                {
                    nickname = AuthRequestData.Nickname,
                    passwordHash = HashUtility.SHA512(AuthRequestData.Password),
                    deviceId = DeviceIdHelper.GetLocalDeviceId(),
                    rememberMe = AuthRequestData.RememberMe
                };

                AuthRequestData.Password = null;

                NetworkClient.Send(loginMsg);
                break;

            case AuthType.Auto:
                NetworkClient.Send(new AutoLoginRequestMessage
                {
                    deviceId = DeviceIdHelper.GetLocalDeviceId()
                });
                break;

            case AuthType.None:
            default:
                break;
        }
    }

    private void OnClientLoginResponse(LoginResponseMessage msg)
    {
        if (msg.success == false)
        {
            ClientReject();
            if (AuthUIController.Instance != null)
                AuthUIController.Instance.ShowLoginPanel(msg.message);

            Debug.LogWarning($"[AuthClient] Login failed: {msg.message}");
            return;
        }

        PlayerData pd;
        try
        {
            pd = JsonUtility.FromJson<PlayerData>(msg.playerJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthClient] Failed to deserialize player data: {ex}");
            ClientReject();
            if (AuthUIController.Instance != null)
                AuthUIController.Instance.ShowLoginPanel("Failed to parse player data");

            return;
        }

        ClientGameState.Instance.Initialize(pd);

        ClientAccept();
        Debug.Log("[AuthClient] Login successful and client state initialized");
    }
}