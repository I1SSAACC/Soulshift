using System;
using System.Security.Cryptography;
using System.Text;

public static class SecurityConstants
{
    public const int Pbkdf2Iterations = 100_000;
    public const int SaltBytes = 16;
    public const int DerivedBytes = 32;
}

public static class HashUtility
{
    public static string SHA512(string input)
    {
        using (var sha = System.Security.Cryptography.SHA512.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }

    public static string CreateSalt()
    {
        byte[] salt = new byte[SecurityConstants.SaltBytes];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            rng.GetBytes(salt);

        return Convert.ToBase64String(salt);
    }

    public static string HashPasswordWithSalt(string passwordHexOrPlain, string saltB64, int iterations = SecurityConstants.Pbkdf2Iterations)
    {
        byte[] passwordBytes = HexOrUtf8ToBytes(passwordHexOrPlain);
        byte[] salt = Convert.FromBase64String(saltB64);

        byte[] derived = Pbkdf2(passwordBytes, salt, iterations, SecurityConstants.DerivedBytes);

        string hashB64 = Convert.ToBase64String(derived);
        return $"{iterations}${saltB64}${hashB64}";
    }

    public static bool VerifyPbkdf2Stored(string stored, string passwordHexOrPlain)
    {
        try
        {
            string[] parts = stored.Split('$');
            if (parts.Length != 3) return false;

            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] storedHash = Convert.FromBase64String(parts[2]);

            byte[] passwordBytes = HexOrUtf8ToBytes(passwordHexOrPlain);
            byte[] derived = Pbkdf2(passwordBytes, salt, iterations, storedHash.Length);

            return CryptographicEquals(storedHash, derived);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Pbkdf2(byte[] passwordBytes, byte[] salt, int iterations, int outputBytes)
    {
        using (Rfc2898DeriveBytes pbkdf = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256))
        {
            return pbkdf.GetBytes(outputBytes);
        }
    }

    private static byte[] HexOrUtf8ToBytes(string input)
    {
        if (string.IsNullOrEmpty(input))
            return
                Array.Empty<byte>();

        if ((input.Length & 1) == 0)
        {
            try
            {
                byte[] hex = HexStringToBytes(input);
                return hex;
            }
            catch { }
        }

        return Encoding.UTF8.GetBytes(input);
    }

    private static byte[] HexStringToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
        int len = hex.Length;
        if ((len & 1) != 0) throw new ArgumentException("Hex string must have even length");
        byte[] bytes = new byte[len / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

        return bytes;
    }

    private static bool CryptographicEquals(byte[] a, byte[] b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];

        return diff == 0;
    }
}