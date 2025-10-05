using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System.Threading;

[DefaultExecutionOrder(-100)]
public class ServerDataManager : MonoBehaviour
{
    public static ServerDataManager Instance { get; private set; }

    public AccountsDb accountsDb;

    private readonly ReaderWriterLockSlim _accountsLock = new ReaderWriterLockSlim();

    private readonly Dictionary<string, AccountEntry> _accountsByGuid = new Dictionary<string, AccountEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, AccountEntry> _accountsByNickname = new Dictionary<string, AccountEntry>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            DbPaths.EnsureDbFoldersExist();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerDataManager] Failed to ensure DB folders: {ex}");
            throw;
        }

        LoadAccounts();
    }

    private void LoadAccounts()
    {
        _accountsLock.EnterWriteLock();
        try
        {
            if (File.Exists(DbPaths.AccountsFile))
            {
                string json = File.ReadAllText(DbPaths.AccountsFile, Encoding.UTF8);
                AccountsDb db = JsonUtility.FromJson<AccountsDb>(json);
                accountsDb = db ?? new AccountsDb();
            }
            else
            {
                accountsDb = new AccountsDb();
                SaveAccountsInternal();
            }

            // rebuild indexes
            _accountsByGuid.Clear();
            _accountsByNickname.Clear();
            foreach (AccountEntry e in accountsDb.Accounts)
            {
                if (string.IsNullOrEmpty(e.Guid) == false)
                    _accountsByGuid[e.Guid] = e;

                if (string.IsNullOrEmpty(e.Nickname) == false)
                    _accountsByNickname[e.Nickname] = e;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerDataManager] Failed to load accounts: {ex}");
            accountsDb = new AccountsDb();
        }
        finally
        {
            _accountsLock.ExitWriteLock();
        }
    }

    private void SaveAccountsInternal()
    {
        string json = JsonUtility.ToJson(accountsDb, true);
        string dir = Path.GetDirectoryName(DbPaths.AccountsFile);
        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);

        string temp = DbPaths.AccountsFile + ".tmp";
        File.WriteAllText(temp, json, Encoding.UTF8);

        if (File.Exists(DbPaths.AccountsFile))
            File.Replace(temp, DbPaths.AccountsFile, null);
        else
            File.Move(temp, DbPaths.AccountsFile);
    }

    public void SaveAccounts()
    {
        _accountsLock.EnterWriteLock();
        try
        {
            SaveAccountsInternal();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerDataManager] Failed to save accounts: {ex}");
            throw;
        }
        finally
        {
            _accountsLock.ExitWriteLock();
        }
    }

    public bool CreateAccount(string email, string nickname, string passwordClientHash, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(passwordClientHash))
        {
            message = "Invalid registration data";
            return false;
        }

        string newGuid = Guid.NewGuid().ToString();
        string pbkdf = CreatePbkdf2FromClientHash(passwordClientHash);

        _accountsLock.EnterUpgradeableReadLock();
        try
        {
            if (_accountsByGuid.Values.Any(a => a.Email != null && a.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                message = "Email already in use";
                return false;
            }

            if (_accountsByNickname.ContainsKey(nickname))
            {
                message = "Nickname already in use";
                return false;
            }

            _accountsLock.EnterWriteLock();
            try
            {
                AccountEntry entry = new AccountEntry
                {
                    Guid = newGuid,
                    Nickname = nickname,
                    Email = email,
                    PasswordHash = pbkdf,
                    IsOnline = false
                };

                accountsDb.Accounts.Add(entry);
                _accountsByGuid[newGuid] = entry;
                _accountsByNickname[nickname] = entry;

                SaveAccountsInternal();
                Debug.Log($"[ServerDataManager] Created account guid={newGuid} nickname={nickname}");
            }
            finally
            {
                _accountsLock.ExitWriteLock();
            }
        }
        finally
        {
            _accountsLock.ExitUpgradeableReadLock();
        }

        try
        {
            LoadOrCreatePlayer(newGuid, email, nickname);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ServerDataManager] Failed to create player data for {newGuid}: {ex}");
        }

        message = "Registered successfully";
        return true;
    }

    public AccountEntry VerifyLogin(string nickname, string passwordClientHash, bool rememberMe, string deviceId, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(passwordClientHash))
        {
            message = "Invalid credentials";
            return null;
        }

        _accountsLock.EnterUpgradeableReadLock();
        try
        {
            if (!_accountsByNickname.TryGetValue(nickname, out AccountEntry entry))
            {
                message = "Incorrect username or password";
                return null;
            }

            bool passwordOk = false;

            if (IsPbkdf2Hash(entry.PasswordHash))
            {
                passwordOk = VerifyPbkdf2(entry.PasswordHash, passwordClientHash);
            }
            else
            {
                if (string.Equals(entry.PasswordHash, passwordClientHash, StringComparison.OrdinalIgnoreCase))
                {
                    passwordOk = true;
                    try
                    {
                        string migrated = CreatePbkdf2FromClientHash(passwordClientHash);
                        _accountsLock.EnterWriteLock();
                        try
                        {
                            entry.PasswordHash = migrated;
                            SaveAccountsInternal();
                        }
                        finally
                        {
                            _accountsLock.ExitWriteLock();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ServerDataManager] Migration to PBKDF2 failed for {nickname}: {ex}");
                    }
                }
            }

            if (passwordOk == false)
            {
                message = "Incorrect username or password";
                return null;
            }

            if (entry.IsOnline)
            {
                message = "This account is already logged in";
                return null;
            }

            _accountsLock.EnterWriteLock();
            try
            {
                entry.IsOnline = true;
                SaveAccountsInternal();
            }
            finally
            {
                _accountsLock.ExitWriteLock();
            }
        }
        finally
        {
            _accountsLock.ExitUpgradeableReadLock();
        }

        if (string.IsNullOrEmpty(deviceId) == false)
        {
            try
            {
                PlayerData pd = LoadOrCreatePlayer(_accountsByNickname[nickname].Guid);
                if (pd.DeviceId == null) pd.DeviceId = new List<string>();
                if (pd.DeviceId.Contains(deviceId) == false)
                {
                    pd.DeviceId.Add(deviceId);
                    SavePlayer(pd);
                    Debug.Log($"[ServerDataManager] Persisted deviceId for guid={_accountsByNickname[nickname].Guid} deviceId={deviceId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ServerDataManager] Failed to persist device id during login: {ex}");
            }
        }

        message = "Login successful";
        return _accountsByNickname[nickname];
    }

    public AccountEntry FindByDeviceId(string deviceId)
    {
        Debug.Log($"[ServerDataManager] FindByDeviceId called for deviceId={deviceId}");
        if (string.IsNullOrEmpty(deviceId)) return null;

        List<AccountEntry> snapshot;
        _accountsLock.EnterReadLock();
        try
        {
            snapshot = accountsDb.Accounts.ToList();
        }
        finally
        {
            _accountsLock.ExitReadLock();
        }

        foreach (AccountEntry entry in snapshot)
        {
            try
            {
                PlayerData pd = LoadOrCreatePlayer(entry.Guid);
                if (pd != null && pd.DeviceId != null && pd.DeviceId.Contains(deviceId))
                {
                    Debug.Log($"[ServerDataManager] FindByDeviceId found guid={entry.Guid} nickname={entry.Nickname}");
                    return entry;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ServerDataManager] Failed reading player data for {entry.Guid}: {ex}");
            }
        }

        Debug.Log($"[ServerDataManager] FindByDeviceId did not find any account for deviceId={deviceId}");
        return null;
    }

    public PlayerData LoadOrCreatePlayer(string guid, string email = "", string nickname = "")
    {
        string path = Path.Combine(DbPaths.PlayersDataFolder, $"{guid}.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            try
            {
                return JsonUtility.FromJson<PlayerData>(json);
            }
            catch (Exception)
            {
                return CreateDefaultPlayer(guid, email, nickname);
            }
        }
        else
        {
            PlayerData pd = CreateDefaultPlayer(guid, email, nickname);
            SavePlayer(pd);
            return pd;
        }
    }

    public PlayerData CreateDefaultPlayer(string guid, string email, string nickname)
    {
        return new PlayerData
        {
            GUID = guid,
            Email = email,
            Nickname = nickname,
            Level = 1,
            Gold = 100
        };
    }

    public void SavePlayer(PlayerData pd)
    {
        try
        {
            string path = Path.Combine(DbPaths.PlayersDataFolder, $"{pd.GUID}.json");
            string json = JsonUtility.ToJson(pd, true);
            File.WriteAllText(path, json, Encoding.UTF8);
            Debug.Log($"[ServerDataManager] Saved player {pd.GUID} to {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerDataManager] Failed to save player {pd.GUID}: {ex}");
            throw;
        }
    }

    public void RemoveDeviceIdFromPlayer(string guid, string deviceId)
    {
        if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(deviceId)) return;

        try
        {
            PlayerData pd = LoadOrCreatePlayer(guid);
            if (pd.DeviceId != null && pd.DeviceId.RemoveAll(d => d == deviceId) > 0)
            {
                SavePlayer(pd);
                Debug.Log($"[ServerDataManager] Removed deviceId {deviceId} from player {guid}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ServerDataManager] RemoveDeviceIdFromPlayer failed: {ex}");
        }
    }

    // Helper methods for external callers
    public void MarkAccountOnline(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return;

        _accountsLock.EnterWriteLock();
        try
        {
            if (_accountsByGuid.TryGetValue(guid, out AccountEntry e))
            {
                e.IsOnline = true;
                SaveAccountsInternal();
                Debug.Log($"[ServerDataManager] MarkAccountOnline guid={guid}");
            }
        }
        finally
        {
            _accountsLock.ExitWriteLock();
        }
    }

    public void MarkAccountOffline(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return;

        _accountsLock.EnterWriteLock();
        try
        {
            if (_accountsByGuid.TryGetValue(guid, out AccountEntry e))
            {
                e.IsOnline = false;
                SaveAccountsInternal();
                Debug.Log($"[ServerDataManager] MarkAccountOffline guid={guid}");
            }
        }
        finally
        {
            _accountsLock.ExitWriteLock();
        }
    }

    // PBKDF2 methods (CreatePbkdf2FromClientHash, VerifyPbkdf2, HexStringToBytes, IsPbkdf2Hash, CryptographicEquals)
    private static string CreatePbkdf2FromClientHash(string clientHashHex)
    {
        byte[] salt = new byte[SecurityConstants.SaltBytes];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            rng.GetBytes(salt);

        byte[] clientBytes = HexStringToBytes(clientHashHex);
        byte[] derived = Pbkdf2(clientBytes, salt, SecurityConstants.Pbkdf2Iterations, SecurityConstants.DerivedBytes);

        string saltB64 = Convert.ToBase64String(salt);
        string hashB64 = Convert.ToBase64String(derived);
        return $"{SecurityConstants.Pbkdf2Iterations}${saltB64}${hashB64}";
    }

    private static bool VerifyPbkdf2(string stored, string clientHashHex)
    {
        try
        {
            string[] parts = stored.Split('$');
            if (parts.Length != 3) return false;
            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] storedHash = Convert.FromBase64String(parts[2]);

            byte[] clientBytes = HexStringToBytes(clientHashHex);
            byte[] derived = Pbkdf2(clientBytes, salt, iterations, storedHash.Length);

            return CryptographicEquals(storedHash, derived);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ServerDataManager] VerifyPbkdf2 failed: {ex}");
            return false;
        }
    }

    private static byte[] Pbkdf2(byte[] passwordBytes, byte[] salt, int iterations, int outputBytes)
    {
        using (Rfc2898DeriveBytes pbkdf = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256))
            return pbkdf.GetBytes(outputBytes);
    }

    private static byte[] HexStringToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
        int len = hex.Length;
        if ((len & 1) != 0) throw new ArgumentException("Hex string must have even length");
        byte[] bytes = new byte[len / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

        return bytes;
    }

    private static bool IsPbkdf2Hash(string stored)
    {
        return !string.IsNullOrEmpty(stored) && stored.Contains("$");
    }

    private static bool CryptographicEquals(byte[] a, byte[] b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }
}