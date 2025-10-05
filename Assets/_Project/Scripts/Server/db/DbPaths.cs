using System.IO;
using UnityEngine;

public static class DbPaths
{
    private static string GetBuildRoot()
    {
#if UNITY_EDITOR
        return Application.dataPath;
#else
        string dir = Path.GetDirectoryName(Application.dataPath);
        return string.IsNullOrEmpty(dir) ? Application.persistentDataPath : dir;
#endif
    }

    private static readonly string s_buildRoot = GetBuildRoot();

    public static readonly string DbFolder = Path.Combine(s_buildRoot, "db");
    public static readonly string PlayerFolder = Path.Combine(DbFolder, "Player");
    public static readonly string AccountsFile = Path.Combine(PlayerFolder, "accounts.json");
    public static readonly string PlayersDataFolder = Path.Combine(PlayerFolder, "PlayersData");
    public static readonly string PromoFile = Path.Combine(DbFolder, "promo.json");

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