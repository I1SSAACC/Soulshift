using System;
using System.Collections.Generic;

[Serializable]
public class AccountEntry
{
    public string Guid;
    public string Nickname;
    public string Email;
    public string PasswordHash;
    public bool IsOnline;
}

[Serializable]
public class AccountsDb
{
    public List<AccountEntry> Accounts = new List<AccountEntry>();
}

[Serializable]
public class PlayerData
{
    public string GUID;
    public List<string> DeviceId = new List<string>();
    public string Nickname;
    public string Email;
    public int Level = 1;
    public int Gold = 0;
    public int Diamonds = 0;
    public List<string> OwnedCharacters = new List<string>();
    public int LevelField = 0;
    public PreferencesData PreferencesData = new PreferencesData();
    public List<string> RedeemedPromoCodes = new List<string>();
    public bool HasLoggedInBefore = false;
}

[Serializable]
public class PreferencesData
{
    public float SFXVolume = 1f;
    public float MusicVolume = 1f;
}