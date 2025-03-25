using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Renard
{
    using Debuger;

    public static class AESGenerator
    {
        public const int EncryptKeyLength = 32;

        public const int EncryptIVLength = 16;

        private const string passChars = @"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ^+-/=*$";

        private static bool _isDebugLog = false;

        private static void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!_isDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(typeof(AESGenerator), logType, methodName, message);
        }

        /// <summary>キー生成</summary>
        public static string GenerateKey(int createLength, int seed, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            try
            {
                if (createLength <= 0 || passChars.Length <= 0)
                    throw new Exception($"create parame error. createLength={createLength}, passLength={passChars.Length}");

                var result = new StringBuilder(createLength);
                var random = new System.Random(seed);
                var index = -1;

                for (int i = 0; i < createLength; i++)
                {
                    index = random.Next(0, passChars.Length);

                    if (index < 0 || passChars.Length <= index)
                        throw new Exception($"not index. passLength={passChars.Length}, index={index}");

                    result.Append(passChars[index]);
                }
                return result.ToString();
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "GeneratePassKey", $"{ex.Message}");
            }
            return string.Empty;
        }

        /// <summary>暗号化</summary>
        public static string Encrypt(string stringCode, string key, string iv, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            try
            {
                if (string.IsNullOrEmpty(key) || key.Length != EncryptKeyLength)
                    throw new Exception($"encryptKey error. length={(key != null ? key.Length : 0)}");

                if (string.IsNullOrEmpty(iv) || iv.Length != EncryptIVLength)
                    throw new Exception($"encryptIV error. length={(iv != null ? iv.Length : 0)}");

                if (string.IsNullOrEmpty(stringCode))
                    throw new Exception("null or empty stringCode.");

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.IV = Encoding.UTF8.GetBytes(iv);
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    byte[] encrypted;
                    using (MemoryStream mStream = new MemoryStream())
                    {
                        using (CryptoStream ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(ctStream))
                            {
                                sw.Write(stringCode);
                            }
                            encrypted = mStream.ToArray();
                        }
                    }

                    return Convert.ToBase64String(encrypted);
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "Encrypt", $"{ex.Message}");
            }
            return string.Empty;
        }

        /// <summary>復号化</summary>
        public static string Decrypt(string encryptCode, string key, string iv, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            try
            {
                if (string.IsNullOrEmpty(key) || key.Length != EncryptKeyLength)
                    throw new Exception($"encryptKey error. length={(key != null ? key.Length : 0)}");

                if (string.IsNullOrEmpty(iv) || iv.Length != EncryptIVLength)
                    throw new Exception($"encryptIV error. length={(iv != null ? iv.Length : 0)}");

                if (string.IsNullOrEmpty(encryptCode))
                    throw new Exception("null or empty encryptCode.");

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.IV = Encoding.UTF8.GetBytes(iv);
                    aesAlg.Mode = CipherMode.CBC;
                    aesAlg.Padding = PaddingMode.PKCS7;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    var plain = string.Empty;
                    using (MemoryStream mStream = new MemoryStream(Convert.FromBase64String(encryptCode)))
                    {
                        using (CryptoStream ctStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(ctStream))
                            {
                                plain = sr.ReadLine();
                            }
                        }
                    }
                    return plain;
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "Decrypt", $"{ex.Message}");
            }
            return string.Empty;
        }
    }
}
