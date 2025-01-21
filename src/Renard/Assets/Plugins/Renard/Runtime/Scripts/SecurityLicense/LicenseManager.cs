using System;
using System.Security.Cryptography;
using System.Text;

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
        private const int partsLength = 4;

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

        private static string ConvertToBase64String(byte[] data)
        {
            try
            {
                if (data == null || data.Length <= 0)
                    throw new Exception("null or empty.");
                return Convert.ToBase64String(data);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "ConvertToBase64String", $"{ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>ライセンス生成</summary>
        public static string GenerateLicense(LicenseConfigAsset configAsset, LicenseData data, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            try
            {
                if (string.IsNullOrEmpty(data.Uuid) || string.IsNullOrEmpty(data.ContentsId))
                    throw new Exception($"licenseData error. deviceId={data.Uuid}, contentsId={data.ContentsId}, expiryDate={data.ExpiryDate:yyyy-MM-dd}");

                var keyContainer = configAsset != null ? configAsset.KeyContainer : string.Empty;
                var licensePassKey = configAsset != null ? configAsset.LicensePassKey : string.Empty;

                if (string.IsNullOrEmpty(keyContainer))
                    throw new Exception($"null or empty licenseConfig. keyContainer={keyContainer}");

                var licenseData = $"{data.Uuid}|{data.ContentsId}|{licensePassKey}|{data.ExpiryDate:yyyy-MM-dd}";
                return SignData(licenseData, CreatePrivateKey(keyContainer));
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "GenerateLicense", $"{ex.Message}");
            }
            return string.Empty;
        }

        private static RSAParameters CreatePrivateKey(string keyContainer)
        {
            try
            {
                RSAParameters privateKey;

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(GetCspParams(keyContainer)))
                {
                    privateKey = rsa.ExportParameters(true);
                    return privateKey;
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "CreatePrivateKey", $"{ex.Message}");
            }
            return default;
        }

        private static string SignData(string licenseData, RSAParameters privateKey)
        {
            try
            {
                if (string.IsNullOrEmpty(licenseData))
                    throw new Exception("null or empty licenseData.");

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(privateKey);

                    var dataBytes = Encoding.UTF8.GetBytes(licenseData);
                    var signedBytes = rsa.SignData(dataBytes, new SHA256Cng());
                    var signedData = ConvertToBase64String(signedBytes);

                    Log(DebugerLogType.Info, "SignData", $"success\n\r{licenseData}\n\r{signedData}");

                    return $"{licenseData}.{signedData}";
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "SignData", $"{ex.Message}");
            }
            return string.Empty;
        }

        /// <summary>ライセンス読込み ※ここでは日付のみチェックする</summary>
        public static LicenseStatusEnum ValidateLicense(LicenseConfigAsset configAsset, string licenseCode, out LicenseData date, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            date = new LicenseData();
            var status = LicenseStatusEnum.None;

            try
            {
                var keyContainer = configAsset != null ? configAsset.KeyContainer : string.Empty;
                var licensePassKey = configAsset != null ? configAsset.LicensePassKey : string.Empty;

                if (string.IsNullOrEmpty(keyContainer))
                    throw new Exception($"null or empty licenseConfig. keyContainer={keyContainer}");

                var licenseData = VerifySignature(licenseCode, keyContainer);
                var dataParts = licenseData.Split('|');

                Log(DebugerLogType.Info, "ValidateLicense", $"{licenseCode}\n\r{licenseData}\n\r{dataParts.Length}={partsLength}");

                if (dataParts.Length == partsLength)
                {
                    // 期限チェック
                    if (DateTime.TryParse(dataParts[3], out date.CreateDate))
                    {
                        date.Uuid = dataParts[0];
                        date.ContentsId = dataParts[1];
                        date.LicensePassKey = dataParts[2];

                        if (date.ExpiryDate > DateTime.Now)
                        {
                            status = LicenseStatusEnum.Success;
                        }
                        else
                        {
                            status = LicenseStatusEnum.Expired;
                        }
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

        private static CspParameters GetCspParams(string keyContainer)
        {
            try
            {
                if (string.IsNullOrEmpty(keyContainer))
                    throw new Exception("null or empty keyContainer.");

                var cspParams = new CspParameters();
                cspParams.KeyContainerName = keyContainer;

                return cspParams;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "GetCspParams", $"{ex.Message}");
            }
            return null;
        }

        private static RSAParameters CreatePublicKey(string keyContainer)
        {
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                using (var rsa = RSA.Create())
                {
                    var bytesRead = 0;
                    rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(keyContainer), out bytesRead);
                    return rsa.ExportParameters(false);
                }
#else
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(GetCspParams(keyContainer)))
                {
                    return rsa.ExportParameters(false);
                }
#endif
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "CreatePublicKey", $"{ex.Message}");
            }
            return default;
        }

        private static string VerifySignature(string signedData, string keyContainer)
        {
            try
            {
                var parts = signedData != null && signedData.Length > 0 ? signedData.Split('.') : null;
                if (parts == null || parts.Length != 2)
                    throw new FormatException($"invalid signed data format. dataLength={(signedData != null ? signedData.Length : 0)}");

                var licenseData = parts[0];
                var signedBytes = Convert.FromBase64String(parts[1]);

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(CreatePublicKey(keyContainer));
                    var dataBytes = Encoding.UTF8.GetBytes(licenseData);
                    var isValid = rsa.VerifyData(dataBytes, new SHA256Cng(), signedBytes);

                    if (!isValid)
                        throw new CryptographicException("signature verification failed");

                    return licenseData;
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "VerifySignature", $"{ex.Message}");
            }
            return string.Empty;
        }
    }
}
