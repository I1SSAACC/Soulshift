using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AccountEntry
{
    [SerializeField] private string _guid;
    [SerializeField] private string _nickname;
    [SerializeField] private string _email;
    [SerializeField] private string _passwordHash;
    [SerializeField] private bool _isOnline;
    [SerializeField] private string _token;

    public AccountEntry() { }

    public string Guid
    {
        get => _guid;
        set => _guid = value;
    }

    public string Nickname
    {
        get => _nickname;
        set => _nickname = value;
    }

    public string Email
    {
        get => _email;
        set => _email = value;
    }

    public string PasswordHash
    {
        get => _passwordHash;
        set => _passwordHash = value;
    }

    public bool IsOnline
    {
        get => _isOnline;
        set => _isOnline = value;
    }

    public string Token
    {
        get => _token;
        set => _token = value;
    }
}

[Serializable]
public class AccountsDb
{
    [SerializeField] private List<AccountEntry> _accounts = new List<AccountEntry>();

    public AccountsDb() { }

    public List<AccountEntry> Accounts
    {
        get => _accounts;
        set => _accounts = value;
    }
}

[Serializable]
public class PlayerData
{
    [SerializeField] private string _guid;
    [SerializeField] private List<string> _deviceIds = new List<string>();
    [SerializeField] private string _nickname;
    [SerializeField] private string _email;
    [SerializeField] private int _level = 1;
    [SerializeField] private int _gold = 0;
    [SerializeField] private int _diamonds = 0;
    [SerializeField] private List<string> _ownedCharacters = new List<string>();
    [SerializeField] private int _levelField = 0;
    [SerializeField] private PreferencesData _preferencesData = new PreferencesData();
    [SerializeField] private List<string> _redeemedPromoCodes = new List<string>();
    [SerializeField] private bool _hasLoggedInBefore = false;

    public PlayerData() { }

    public string GUID
    {
        get => _guid;
        set => _guid = value;
    }

    public List<string> DeviceIds
    {
        get => _deviceIds;
        set => _deviceIds = value;
    }

    public string Nickname
    {
        get => _nickname;
        set => _nickname = value;
    }

    public string Email
    {
        get => _email;
        set => _email = value;
    }

    public int Level
    {
        get => _level;
        set => _level = value;
    }

    public int Gold
    {
        get => _gold;
        set => _gold = value;
    }

    public int Diamonds
    {
        get => _diamonds;
        set => _diamonds = value;
    }

    public List<string> OwnedCharacters
    {
        get => _ownedCharacters;
        set => _ownedCharacters = value;
    }

    public int LevelField
    {
        get => _levelField;
        set => _levelField = value;
    }

    public PreferencesData PreferencesData
    {
        get => _preferencesData;
        set => _preferencesData = value;
    }

    public List<string> RedeemedPromoCodes
    {
        get => _redeemedPromoCodes;
        set => _redeemedPromoCodes = value;
    }

    public bool HasLoggedInBefore
    {
        get => _hasLoggedInBefore;
        set => _hasLoggedInBefore = value;
    }
}

[Serializable]
public class PreferencesData
{
    [SerializeField] private float _sfxVolume = 1f;
    [SerializeField] private float _musicVolume = 1f;

    public PreferencesData() { }

    public float SFXVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = value;
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set => _musicVolume = value;
    }
}