using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Plugins.RZDAds.ApiSystem
{
    public static class Encryptor
    {
        public static EncryptedRequest Encrypt<T>(T data, string rsaPublicPem)
        {
            // 1. Сериализуем объект в JSON
            string json = JsonUtility.ToJson(data);
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);

            // 2. Генерируем случайный AES ключ (256 бит)
            byte[] aesKey = new byte[32];
            RandomNumberGenerator.Fill(aesKey);

            // 3. Генерируем случайный IV (12 байт для AES-GCM)
            byte[] iv = new byte[12];
            RandomNumberGenerator.Fill(iv);

            // 4. Шифруем payload с помощью AES-GCM
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[16]; // AES-GCM тег 16 байт
            using (var aesGcm = new AesGcm(aesKey))
            {
                aesGcm.Encrypt(iv, plainBytes, cipherBytes, tag);
            }

            // 5. Объединяем ciphertext + tag
            byte[] combined = new byte[cipherBytes.Length + tag.Length];
            Buffer.BlockCopy(cipherBytes, 0, combined, 0, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, combined, cipherBytes.Length, tag.Length);
            string dataBase64 = Convert.ToBase64String(combined);
            string ivBase64 = Convert.ToBase64String(iv);

            // 6. Шифруем AES ключ RSA (OAEP SHA-256)
            byte[] aesKeyEncrypted = EncryptAesKeyWithRsa(aesKey, rsaPublicPem);
            string keyBase64 = Convert.ToBase64String(aesKeyEncrypted);

            return new EncryptedRequest
            {
                key = keyBase64,
                iv = ivBase64,
                data = dataBase64
            };
        }

        private static byte[] EncryptAesKeyWithRsa(byte[] data, string pem)
        {
            // Убираем заголовки PEM
            string key = pem.Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "");
            byte[] keyBytes = Convert.FromBase64String(key);

            using (var rsa = new RSACryptoServiceProvider())
            {
                // Импортируем X.509 ключ
                rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
                return rsa.Encrypt(data, true); // true = OAEP padding
            }
        }
    }
}