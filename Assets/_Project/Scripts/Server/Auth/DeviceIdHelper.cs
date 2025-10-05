using System;
using UnityEngine;

public static class DeviceIdHelper
{
    private const string PlayerPrefsKey = "localDeviceGuid";

    public static string GetLocalDeviceId()
    {
        string id = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(PlayerPrefsKey, id);
            PlayerPrefs.Save();
            Debug.Log($"[DeviceIdHelper] Generated new local device id: {id}");
        }

        return id;
    }
}