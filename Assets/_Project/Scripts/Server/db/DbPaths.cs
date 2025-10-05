using System.IO;
using UnityEngine;

public static class DbPaths
{
    public static readonly string DbFolder;
    public static readonly string PlayerFolder;
    public static readonly string AccountsFile;
    public static readonly string PlayersDataFolder;
    public static readonly string PromoFile;

    private const string DbFolderName = "db";
    private const string PlayerFolderName = "Player";
    private const string AccountsFileName = "accounts.json";
    private const string PlayersDataFolderName = "PlayersData";
    private const string PromoFileName = "promo.json";

    static DbPaths()
    {
        string buildRoot = GetBuildRoot();

        DbFolder = Path.Combine(buildRoot, DbFolderName);
        PlayerFolder = Path.Combine(DbFolder, PlayerFolderName);
        AccountsFile = Path.Combine(PlayerFolder, AccountsFileName);
        PlayersDataFolder = Path.Combine(PlayerFolder, PlayersDataFolderName);
        PromoFile = Path.Combine(DbFolder, PromoFileName);
    }

    private static string GetBuildRoot()
    {
#if UNITY_EDITOR
        return Application.dataPath;
#else
        string parent = Path.GetDirectoryName(Application.dataPath);

        if (string.IsNullOrEmpty(parent) == false)
            return parent;

        return Application.persistentDataPath;
#endif
    }

    public static void EnsureDbFoldersExist()
    {
        try
        {
            if (Directory.Exists(DbFolder) == false)
                Directory.CreateDirectory(DbFolder);

            if (Directory.Exists(PlayerFolder) == false)
                Directory.CreateDirectory(PlayerFolder);

            if (Directory.Exists(PlayersDataFolder) == false)
                Directory.CreateDirectory(PlayersDataFolder);

            Debug.Log($"[DbPaths] DB folders ready at {DbFolder}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DbPaths] Failed to create DB folders: {ex}");
            throw;
        }
    }
}