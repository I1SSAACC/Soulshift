using System.IO;
using UnityEngine;

public static class DbPaths
{
    public static string DbFolder => Path.Combine(Application.dataPath, "db");
    public static string AccountsFile => Path.Combine(DbFolder, "accounts.json");
    public static string TokensFile => Path.Combine(DbFolder, "tokens.json");

    public static string GetPlayerFilePathByGuid(string guid)
    {
        return Path.Combine(DbFolder, "players", guid + ".json");
    }

    public static void EnsureDbFoldersExist()
    {
        if (!Directory.Exists(DbFolder)) Directory.CreateDirectory(DbFolder);
        string players = Path.Combine(DbFolder, "players");
        if (!Directory.Exists(players)) Directory.CreateDirectory(players);
    }
}