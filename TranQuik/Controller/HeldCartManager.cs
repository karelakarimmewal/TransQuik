using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TranQuik.Model;

public class HeldCartManager
{
    private string directoryPath;
    private string filePath;
    private readonly byte[] key;
    private readonly byte[] iv;

    public HeldCartManager()
    {
        // Combine the base directory with "Logs/HB"
        directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        // Ensure the directory exists
        Directory.CreateDirectory(directoryPath);

        // Set the file path
        filePath = Path.Combine(directoryPath, "hcdatas.json");

        // Use a static key and IV for encryption/decryption
        // In a real application, you should securely generate and store these values
        key = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"); // 32 bytes for AES-256
        iv = Encoding.UTF8.GetBytes("abcdef9876543210"); // 16 bytes for AES
    }

    public void SaveHeldCarts(Dictionary<DateTime, HeldCart> heldCarts)
    {
        string json = JsonSerializer.Serialize(heldCarts);
        byte[] encryptedData = EncryptStringToBytes_Aes(json, key, iv);
        File.WriteAllBytes(filePath, encryptedData);
    }

    public Dictionary<DateTime, HeldCart> LoadHeldCarts()
    {
        if (File.Exists(filePath))
        {
            byte[] encryptedData = File.ReadAllBytes(filePath);
            string json = DecryptStringFromBytes_Aes(encryptedData, key, iv);
            return JsonSerializer.Deserialize<Dictionary<DateTime, HeldCart>>(json);
        }
        else
        {
            return new Dictionary<DateTime, HeldCart>();
        }
    }

    private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] iv)
    {
        if (plainText == null || plainText.Length <= 0)
            throw new ArgumentNullException(nameof(plainText));
        if (key == null || key.Length <= 0)
            throw new ArgumentNullException(nameof(key));
        if (iv == null || iv.Length <= 0)
            throw new ArgumentNullException(nameof(iv));

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return msEncrypt.ToArray();
                }
            }
        }
    }

    private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key, byte[] iv)
    {
        if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException(nameof(cipherText));
        if (key == null || key.Length <= 0)
            throw new ArgumentNullException(nameof(key));
        if (iv == null || iv.Length <= 0)
            throw new ArgumentNullException(nameof(iv));

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }
}