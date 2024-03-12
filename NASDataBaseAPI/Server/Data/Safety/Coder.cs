﻿using NASDatabase.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace NASDatabase.Server.Data.Safety
{
    public class SimpleEncryptor : IEncoder
    {
        public static string GenerateRandomKey(int keySize)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] keyData = new byte[keySize / 8];
                rng.GetBytes(keyData);
                return BitConverter.ToString(keyData).Replace("-", "");
            }
        }

        /// <summary>
        /// Метод для шифрования текста
        /// </summary>
        /// <param name="text"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Encode(string text, string key)
        {
            if (key == " ")
                return text;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = new byte[16]; // Используем нулевой вектор инициализации для простоты

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(text);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// Метод для дешифрования текста
        /// </summary>
        /// <param name="encryptedText"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Decode(string encryptedText, string key)
        {
            if (key == " ")
                return encryptedText;
            if (encryptedText == " " || encryptedText.Length == 0)
                return encryptedText;
            try
            {
                byte[] decodedBytes = Convert.FromBase64String(encryptedText);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.IV = new byte[16]; // Используем нулевой вектор инициализации для простоты

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (var msDecrypt = new System.IO.MemoryStream(Convert.FromBase64String(encryptedText)))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (FormatException ex)
            {
                throw new Exception($"Error decoding Base64: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}

