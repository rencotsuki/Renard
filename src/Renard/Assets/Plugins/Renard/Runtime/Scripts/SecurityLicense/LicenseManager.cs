using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Renard
{
    public enum LicenseStatusEnum : int
    {
        None = 0,

        /// <summary>正常</summary>
        Success,
        /// <summary>ファイル不明</summary>
        NotFile,
        /// <summary>不正</summary>
        Injustice,
        /// <summary>有効期限切れ</summary>
        Expired,
    }

    public struct LicenseData
    {
        public string Uuid;
        public string ContentsId;
        public string LicensePassKey;
        public DateTime CreateDate;
        public int ValidityDays;

        public DateTime ExpiryDate => CreateDate.AddDays(ValidityDays);
    }
}

namespace Renard.License
{
    using Debuger;

    public static class LicenseManager
    {
        public const int EncryptKeyLength = 32;
        public const int EncryptIVLength = 16;

        private const int partsLength = 5;

        private static bool _isDebugLog = false;

        private static void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!_isDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(typeof(LicenseManager), logType, methodName, message);
        }

        private const string base64Pattern = @"^[A-Za-z0-9+/=]*$";

        private const string passChars = @"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ^+-/=*$";

        /// <summary></summary>
        public static bool IsValidBase64String(string input)
        {
            try
            {
                return Regex.IsMatch(input, base64Pattern);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "IsValidBase64String", $"{ex.Message}");
            }
            return false;
        }

        /// <summary></summary>
        public static string GenerateContentsId() => Application.productName;

        /// <summary></summary>
        public static string GeneratePassKey(int createLength, int seed)
            => GeneratePassKey(createLength, seed, _isDebugLog);

        /// <summary></summary>
        public static string GeneratePassKey(int createLength, int seed, bool isDebugLog)
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

        /// <summary></summary>
        public static string EncryptCode(string stringCode, string key, string iv)
            => EncryptCode(stringCode, key, iv, _isDebugLog);

        /// <summary>暗号化</summary>
        public static string EncryptCode(string stringCode, string key, string iv, bool isDebugLog)
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
                Log(DebugerLogType.Warning, "EncryptCode", $"{ex.Message}");
            }
            return string.Empty;
        }

        /// <summary></summary>
        public static string DecryptCode(string stringCode, string key, string iv)
            => EncryptCode(stringCode, key, iv, _isDebugLog);

        /// <summary>復号化</summary>
        public static string DecryptCode(string encryptCode, string key, string iv, bool isDebugLog)
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
                Log(DebugerLogType.Warning, "DecryptCode", $"{ex.Message}");
            }
            return string.Empty;
        }

        /// <summary></summary>
        public static string GenerateLicense(LicenseConfigAsset configAsset, LicenseData data)
            => GenerateLicense(configAsset, data, _isDebugLog);

        /// <summary>ライセンス生成</summary>
        public static string GenerateLicense(LicenseConfigAsset configAsset, LicenseData data, bool isDebugLog)
        {
            _isDebugLog = isDebugLog;

            try
            {
                if (string.IsNullOrEmpty(data.Uuid) || string.IsNullOrEmpty(data.ContentsId))
                    throw new Exception($"licenseData error. deviceId={data.Uuid}, contentsId={data.ContentsId}, createDate={data.CreateDate:yyyy-MM-dd}, ValidityDays={data.ValidityDays}");

                return $"{data.Uuid}|{data.ContentsId}|{(configAsset != null ? configAsset.LicensePassKey : string.Empty)}|{data.CreateDate:yyyy-MM-dd}|{data.ValidityDays}";
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "GenerateLicense", $"{ex.Message}");
            }
            return string.Empty;
        }

        /// <summary>ライセンス読込み ※ここでは日付のみチェックする</summary>
        public static LicenseStatusEnum ValidateLicense(LicenseConfigAsset configAsset, string licenseCode, out LicenseData data)
            => ValidateLicense(configAsset, licenseCode, out data, _isDebugLog);

        /// <summary>ライセンス読込み ※ここでは日付のみチェックする</summary>
        public static LicenseStatusEnum ValidateLicense(LicenseConfigAsset configAsset, string licenseCode, out LicenseData date, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            date = new LicenseData();
            var status = LicenseStatusEnum.None;

            try
            {
                var licensePassKey = configAsset != null ? configAsset.LicensePassKey : string.Empty;
                var dataParts = licenseCode.Split('|');

                Log(DebugerLogType.Info, "ValidateLicense", $"{licenseCode}\n\r{dataParts.Length}={partsLength}");

                if (dataParts.Length == partsLength)
                {
                    // 作成日取得
                    if (!DateTime.TryParse(dataParts[3], out date.CreateDate))
                        throw new Exception("create time error.");

                    if (!int.TryParse(dataParts[4], out date.ValidityDays))
                        throw new Exception("failed get validityDays.");

                    date.Uuid = dataParts[0];
                    date.ContentsId = dataParts[1];
                    date.LicensePassKey = dataParts[2];

                    if (date.ValidityDays > 0 && date.ExpiryDate > DateTime.Now)
                    {
                        status = LicenseStatusEnum.Success;
                    }
                    else
                    {
                        status = LicenseStatusEnum.Expired;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "ValidateLicense", $"{ex.Message}");

                date.CreateDate = DateTime.MinValue;
                status = LicenseStatusEnum.Injustice;
            }
            return status;
        }
    }
}
