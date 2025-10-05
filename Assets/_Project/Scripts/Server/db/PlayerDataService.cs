using System.IO;
using UnityEngine;
using Mirror;

public class PlayerDataService
{
    public const string PlayersFolderName = "PlayersData";

    private static readonly PlayerDataService s_instance = new PlayerDataService();

    private PlayerDataService() { }

    public static PlayerDataService Instance => s_instance;

    public PlayerData LoadPlayerByGuid(string guid)
    {
        if (NetworkServerActiveCheck() == false)
            return null;

        string path = GetPlayerFilePath(guid);

        if (File.Exists(path) == false)
            return null;

        string json = File.ReadAllText(path);

        PlayerData playerData = Utils.FromJson<PlayerData>(json);

        return playerData;
    }

    public void SavePlayer(PlayerData data)
    {
        if (NetworkServerActiveCheck() == false)
            return;

        if (data == null)
            return;

        string path = GetPlayerFilePath(data.GUID);

        string json = Utils.ToJson(data, true);

        File.WriteAllText(path, json);
    }

    public string CreatePlayerAndReturnGuid(string login, string email, string passwordHash)
    {
        if (NetworkServerActiveCheck() == false)
            return string.Empty;

        string guid = Utils.GenerateGuid();

        PlayerData playerData = new PlayerData
        {
            GUID = guid,
            Nickname = login,
            Email = email,
            Level = 1,
            Gold = 0,
            Diamonds = 0
        };

        EnsurePlayersFolderExists();

        SavePlayer(playerData);

        return guid;
    }

    public bool TryApplyGoldChange(string guid, int delta)
    {
        if (NetworkServerActiveCheck() == false)
            return false;

        PlayerData player = LoadPlayerByGuid(guid);

        if (player == null)
            return false;

        long newGold = (long)player.Gold + delta;

        if (newGold < 0)
            return false;

        player.Gold = (int)newGold;

        SavePlayer(player);

        return true;
    }

    public void AddDeviceId(string guid, string deviceId)
    {
        if (NetworkServerActiveCheck() == false)
            return;

        PlayerData player = LoadPlayerByGuid(guid);

        if (player == null)
            return;

        if (player.DeviceIds.Contains(deviceId) == false)
            player.DeviceIds.Add(deviceId);

        SavePlayer(player);
    }

    private static string GetPlayerFilePath(string guid)
    {
        return Path.Combine(DbPaths.PlayersDataFolder, $"{guid}.json");
    }

    private static void EnsurePlayersFolderExists()
    {
        DbPaths.EnsureDbFoldersExist();
    }

    private static bool NetworkServerActiveCheck()
    {
        return Mirror.NetworkServer.active;
    }
}