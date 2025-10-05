using System;
using System.Collections.Generic;
using System.IO;
using Mirror;
using UnityEngine;
using PromoCodes;

[DefaultExecutionOrder(-90)]
public class PromoCodeManager : MonoBehaviour
{
    public static PromoCodeManager Instance { get; private set; }

    [SerializeField] private List<PromoCodeEntry> _promoCodes = new List<PromoCodeEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadOrCreatePromoFile();

        if (NetworkServer.active)
            NetworkServer.RegisterHandler<RedeemPromoRequest>(OnServerRedeemPromo, false);
    }

    private void LoadOrCreatePromoFile()
    {
        try
        {
            if (File.Exists(DbPaths.PromoFile))
            {
                string json = File.ReadAllText(DbPaths.PromoFile, System.Text.Encoding.UTF8);
                PromoCodeList wrapper = JsonUtility.FromJson<PromoCodeList>(json);
                if (wrapper != null && wrapper.codes != null)
                {
                    _promoCodes = wrapper.codes;
                    return;
                }

                _promoCodes = new List<PromoCodeEntry>();
                return;
            }

            _promoCodes = new List<PromoCodeEntry>
            {
                new PromoCodeEntry
                {
                    code = "ALPHA",
                    rewards = new List<RewardOption>
                    {
                        new RewardOption { type = RewardType.Gold, amount = 500 }
                    }
                }
            };

            SavePromoCodes();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PromoCodeManager] Failed to load or create promo file: {ex}");
            _promoCodes = new List<PromoCodeEntry>();
        }
    }

    public void SavePromoCodes()
    {
        try
        {
            PromoCodeList wrapper = new PromoCodeList { codes = _promoCodes };
            string json = JsonUtility.ToJson(wrapper, true);

            string dir = Path.GetDirectoryName(DbPaths.PromoFile);
            if (string.IsNullOrEmpty(dir) == false && Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);

            File.WriteAllText(DbPaths.PromoFile, json, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PromoCodeManager] Failed to save promo codes: {ex}");
            throw;
        }
    }

    private void OnServerRedeemPromo(NetworkConnectionToClient conn, RedeemPromoRequest req)
    {
        if (conn == null)
            return;

        if (string.IsNullOrEmpty(req.code))
        {
            conn.Send(new RedeemPromoResponse { success = false, message = "Invalid code" });
            return;
        }

        CustomNetworkManager nm = NetworkManager.singleton as CustomNetworkManager;
        if (nm == null || nm.TryGetGuid(conn, out string guid) == false || string.IsNullOrEmpty(guid))
        {
            conn.Send(new RedeemPromoResponse
            {
                success = false,
                message = "Unknown client"
            });

            return;
        }

        ServerDataManager dm = ServerDataManager.Instance;
        if (dm == null)
        {
            conn.Send(new RedeemPromoResponse
            {
                success = false,
                message = "Server error"
            });

            return;
        }

        PlayerData pd;
        try
        {
            pd = dm.LoadOrCreatePlayer(guid);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PromoCodeManager] Failed to load player for guid {guid}: {ex}");
            conn.Send(new RedeemPromoResponse
            {
                success = false,
                message = "Player data error"
            });

            return;
        }

        if (ValidateCode(req.code, pd, out PromoCodeEntry entry, out string validateMessage) == false)
        {
            conn.Send(new RedeemPromoResponse
            {
                success = false,
                message = validateMessage
            });

            return;
        }

        if (entry.rewards == null || entry.rewards.Count == 0)
        {
            conn.Send(new RedeemPromoResponse
            {
                success = false,
                message = "Promo code has no rewards"
            });

            return;
        }

        RewardOption option = entry.rewards[0];
        try
        {
            ApplyReward(pd, entry, option);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PromoCodeManager] Failed to apply reward for guid {guid}: {ex}");
            conn.Send(new RedeemPromoResponse
            {
                success = false,
                message = "Failed to apply reward"
            });

            return;
        }

        conn.Send(new RedeemPromoResponse
        {
            success = true,
            message = $"You received {option.amount} {option.type}",
            rewardType = option.type,
            amount = option.amount
        });
    }

    private bool ValidateCode(string code, PlayerData pd, out PromoCodeEntry entry, out string message)
    {
        entry = null;
        message = string.Empty;

        if (string.IsNullOrEmpty(code))
        {
            message = "Invalid code";
            return false;
        }

        string normalized = code.Trim().ToUpperInvariant();

        entry = _promoCodes.Find(e => string.Equals(e.code, normalized, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            message = "Gift code not found.";
            return false;
        }

        if (pd == null)
        {
            message = "Player data missing";
            return false;
        }

        if (pd.RedeemedPromoCodes != null && pd.RedeemedPromoCodes.Contains(entry.code))
        {
            message = "You have already used this promo code.";
            return false;
        }

        message = "OK";
        return true;
    }

    private void ApplyReward(PlayerData pd, PromoCodeEntry entry, RewardOption option)
    {
        if (pd == null || entry == null || option == null)
            throw new ArgumentNullException();

        switch (option.type)
        {
            case RewardType.Gold:
                pd.Gold += option.amount;
                break;
            case RewardType.Diamonds:
                pd.Diamonds += option.amount;
                break;
            default:
                Debug.LogWarning($"[PromoCodeManager] Unknown reward type {option.type}");
                break;
        }

        if (pd.RedeemedPromoCodes == null)
            pd.RedeemedPromoCodes = new List<string>();

        pd.RedeemedPromoCodes.Add(entry.code);

        ServerDataManager.Instance.SavePlayer(pd);
    }
}