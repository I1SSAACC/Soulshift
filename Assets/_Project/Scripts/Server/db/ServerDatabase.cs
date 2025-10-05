using System.IO;
using UnityEngine;
using Mirror;
using System.Text;

public class ServerDatabase : NetworkBehaviour
{
    public const string AccountsDbFileName = "accounts.json";

    [SerializeField] private bool _autoCreateOnServerStart = true;

    private void Awake()
    {
        if (Application.isEditor)
            DbPaths.EnsureDbFoldersExist();
    }

    public override void OnStartServer()
    {
        if (_autoCreateOnServerStart == false)
            return;

        EnsureDatabaseExists();
    }

    private void EnsureDatabaseExists()
    {
        DbPaths.EnsureDbFoldersExist();

        if (File.Exists(DbPaths.AccountsFile) == false)
        {
            AccountsDb accountsDb = new AccountsDb();
            Utils.WriteJsonToFile(accountsDb, DbPaths.AccountsFile, true);
            Debug.Log($"[ServerDatabase] Created accounts DB at {DbPaths.AccountsFile}");
            return;
        }

        Debug.Log($"[ServerDatabase] Accounts DB already exists at {DbPaths.AccountsFile}");
    }
}