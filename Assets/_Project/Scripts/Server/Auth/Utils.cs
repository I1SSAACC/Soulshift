using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class Utils
{
    public static string ToJson<T>(T obj, bool pretty = false) => JsonUtility.ToJson(obj, pretty);

    public static T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);

    public static string ComputeSha256Hash(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(raw);
            byte[] hash = sha256.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    public static string GenerateGuid() => System.Guid.NewGuid().ToString();

    public static void WriteJsonToFile<T>(T obj, string filePath, bool pretty = true)
    {
        string json = ToJson(obj, pretty);
        string dir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(dir) == false && Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
}