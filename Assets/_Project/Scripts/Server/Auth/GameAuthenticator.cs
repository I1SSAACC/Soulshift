using System.IO;
using System.Text;
using Mirror;
using UnityEngine;

public struct AuthRequest : NetworkMessage
{
    public bool IsRegistration;
    public string Email;
    public string Login;
    public string Password;
    public string DeviceId;
    public string Token;
}

public struct AuthResponse : NetworkMessage
{
    public bool Success;
    public string Message;
    public string PlayerGuid;
    public string Token;
}

public static class PlayerPrefsKeys
{
    public const string AuthTokenKey = "auth_token";
}

public static class NetworkAuthBridge
{
    public static void SendAuthRequest(AuthRequest request) => NetworkClient.Send(request);
}

public class GameAuthenticator : NetworkAuthenticator
{
    public const string DeviceIdKey = "device_id";

    [SerializeField] private AuthUI _authUI;
    [SerializeField] private CustomNetworkManager _networkManager;

    private void Awake()
    {
        NetworkClient.RegisterHandler<AuthResponse>(OnClientAuthResponse, false);
        NetworkClient.RegisterHandler<AuthRequest>(OnClientAuthRequestFromUI, false);
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<GetPlayerDataRequest>(OnServerReceiveGetPlayerDataRequest, false);
        NetworkServer.RegisterHandler<AuthRequest>(OnServerReceiveAuthRequest, false);
        DbPaths.EnsureDbFoldersExist();

        if (!File.Exists(DbPaths.AccountsFile))
        {
            var accountsDb = new AccountsDb();
            Utils.WriteJsonToFile(accountsDb, DbPaths.AccountsFile, true);
        }
    }

    public override void OnStartClient() { }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn) { }

    public override void OnClientAuthenticate()
    {
        TryAutoLoginWithToken();
    }

    private void TryAutoLoginWithToken()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsKeys.AuthTokenKey) == false) return;
        string savedToken = PlayerPrefs.GetString(PlayerPrefsKeys.AuthTokenKey);
        if (string.IsNullOrEmpty(savedToken)) return;

        AuthRequest autoRequest = new AuthRequest
        {
            IsRegistration = false,
            Email = string.Empty,
            Login = string.Empty,
            Password = string.Empty,
            DeviceId = SystemInfo.deviceUniqueIdentifier,
            Token = savedToken
        };

        NetworkAuthBridge.SendAuthRequest(autoRequest);
    }

    private void OnClientAuthRequestFromUI(AuthRequest msg) => NetworkAuthBridge.SendAuthRequest(msg);

    private void OnServerReceiveAuthRequest(NetworkConnectionToClient conn, AuthRequest msg)
    {
        if (msg.IsRegistration) { HandleRegistration(conn, msg); return; }
        if (string.IsNullOrEmpty(msg.Token) == false) { HandleTokenLogin(conn, msg); return; }
        HandleLogin(conn, msg);
    }

    private void HandleRegistration(NetworkConnectionToClient conn, AuthRequest msg)
    {
        if (string.IsNullOrEmpty(msg.Login) || string.IsNullOrEmpty(msg.Password) || string.IsNullOrEmpty(msg.Email))
        {
            conn.Send(new AuthResponse { Success = false, Message = "Missing fields", PlayerGuid = string.Empty, Token = string.Empty });
            return;
        }

        if (File.Exists(DbPaths.AccountsFile) == false)
        {
            conn.Send(new AuthResponse { Success = false, Message = "Accounts DB missing", PlayerGuid = string.Empty, Token = string.Empty });
            return;
        }

        string accountsJson = File.ReadAllText(DbPaths.AccountsFile, Encoding.UTF8);
        AccountsDb accountsDb = Utils.FromJson<AccountsDb>(accountsJson) ?? new AccountsDb();

        bool exists = accountsDb.Accounts.Exists(a => a.Nickname == msg.Login || a.Email == msg.Email);
        if (exists)
        {
            conn.Send(new AuthResponse { Success = false, Message = "Account already exists", PlayerGuid = string.Empty, Token = string.Empty });
            return;
        }

        string guid = PlayerDataService.Instance.CreatePlayerAndReturnGuid(msg.Login, msg.Email, Utils.ComputeSha256Hash(msg.Password));

        AccountEntry newEntry = new AccountEntry
        {
            Guid = guid,
            Nickname = msg.Login,
            Email = msg.Email,
            PasswordHash = Utils.ComputeSha256Hash(msg.Password),
            IsOnline = true,
            Token = string.Empty
        };

        string token = Utils.GenerateGuid();
        newEntry.Token = token;

        accountsDb.Accounts.Add(newEntry);

        // save accounts db (pretty + utf8)
        Utils.WriteJsonToFile(accountsDb, DbPaths.AccountsFile, true);

        AuthResponse success = new AuthResponse { Success = true, Message = "Registration successful", PlayerGuid = guid, Token = token };
        conn.Send(success);
        ServerAccept(conn);
    }

    private void HandleLogin(NetworkConnectionToClient conn, AuthRequest msg)
    {
        if (string.IsNullOrEmpty(msg.Login))
        {
            conn.Send(new AuthResponse { Success = false, Message = "Missing login", PlayerGuid = string.Empty, Token = string.Empty });
            return;
        }

        if (File.Exists(DbPaths.AccountsFile) == false)
        {
            conn.Send(new AuthResponse { Success = false, Message = "Accounts DB missing", PlayerGuid = string.Empty, Token = string.Empty });
            return;
        }

        string accountsJson = File.ReadAllText(DbPaths.AccountsFile, Encoding.UTF8);
        AccountsDb accountsDb = Utils.FromJson<AccountsDb>(accountsJson) ?? new AccountsDb();

        AccountEntry account = accountsDb.Accounts.Find(a => a.Nickname == msg.Login || a.Email == msg.Login);
        if (account == null)
        {
            conn.Send(new AuthResponse { Success = false, Message = "Account not found", PlayerGuid = string.Empty, Token = string.Empty });
            return;
        }

        if (string.IsNullOrEmpty(account.PasswordHash) == false && string.IsNullOrEmpty(msg.Password) == false)
        {
            string providedHash = Utils.ComputeSha256Hash(msg.Password);
            if (account.PasswordHash != providedHash)
            {
                conn.Send(new AuthResponse { Success = false, Message = "Invalid password", PlayerGuid = string.Empty, Token = string.Empty });
                return;
            }
        }

        account.IsOnline = true;

        string token = Utils.GenerateGuid();
        account.Token = token;

        // persist accounts db (IsOnline and Token changed)
        Utils.WriteJsonToFile(accountsDb, DbPaths.AccountsFile, true);

        if (string.IsNullOrEmpty(msg.DeviceId) == false)
            PlayerDataService.Instance.AddDeviceId(account.Guid, msg.DeviceId);

        AuthResponse success = new AuthResponse { Success = true, Message = "Login successful", PlayerGuid = account.Guid, Token = token };
        conn.Send(success);
        ServerAccept(conn);
    }

    private void HandleTokenLogin(NetworkConnectionToClient conn, AuthRequest msg)
    {
        if (File.Exists(DbPaths.AccountsFile) == false)
        {
            conn.Send(new AuthResponse { Success = false, Message = "Accounts DB missing", PlayerGuid = string.Empty, Token = string.Empty });
            return;
        }

        string accountsJson = File.ReadAllText(DbPaths.AccountsFile, Encoding.UTF8);
        AccountsDb accountsDb = Utils.FromJson<AccountsDb>(accountsJson) ?? new AccountsDb();

        // find account by token in accountsDb
        AccountEntry account = accountsDb.Accounts.Find(a => a.Token == msg.Token);

        if (account == null)
        {
            conn.Send(new AuthResponse { Success = false, Message = "Invalid token", PlayerGuid = string.Empty, Token = string.Empty });
            return;
        }

        account.IsOnline = true;
        string refreshedToken = Utils.GenerateGuid();
        account.Token = refreshedToken;

        Utils.WriteJsonToFile(accountsDb, DbPaths.AccountsFile, true);

        if (string.IsNullOrEmpty(msg.DeviceId) == false)
            PlayerDataService.Instance.AddDeviceId(account.Guid, msg.DeviceId);

        AuthResponse success = new AuthResponse { Success = true, Message = "Token login successful", PlayerGuid = account.Guid, Token = refreshedToken };
        conn.Send(success);
        ServerAccept(conn);
    }

    private void OnClientAuthResponse(AuthResponse msg)
    {
        if (_authUI != null)
            _authUI.ShowLoginFeedback(msg.Message);

        if (msg.Success)
        {
            if (string.IsNullOrEmpty(msg.Token) == false)
            {
                PlayerPrefs.SetString(PlayerPrefsKeys.AuthTokenKey, msg.Token);
                PlayerPrefs.Save();
            }

            ClientAccept();
        }
    }

    private void OnServerReceiveGetPlayerDataRequest(NetworkConnectionToClient conn, GetPlayerDataRequest msg)
    {
        DbPaths.EnsureDbFoldersExist();

        PlayerData pd = PlayerDataService.Instance.LoadPlayerByGuid(msg.PlayerGuid);

        GetPlayerDataResponse resp = new GetPlayerDataResponse();

        if (pd == null)
        {
            resp.Success = false;
            resp.ErrorMessage = "Player data not found";
            resp.PlayerDataJson = string.Empty;
            conn.Send(resp);
            return;
        }

        resp.Success = true;
        resp.ErrorMessage = string.Empty;
        resp.PlayerDataJson = Utils.ToJson(pd, true);
        conn.Send(resp);
    }
}