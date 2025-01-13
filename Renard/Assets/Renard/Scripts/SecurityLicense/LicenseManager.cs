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
        public static bool IsDebugLog => false;

        private const int partsLength = 4;

        private static void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!IsDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(typeof(LicenseManager), logType, methodName, message);
        }

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

        /*
         * 配布側のアプリでは動作させない
         */

        /// <summary>ライセンス生成</summary>
        public static string GenerateLicense(LicenseConfigAsset configAsset, LicenseData data)
        {
            try
            {
                if (string.IsNullOrEmpty(data.Uuid) || string.IsNullOrEmpty(data.ContentsId))
                    throw new Exception($"licenseData error. deviceId={data.Uuid}, contentsId={data.ContentsId}, expiryDate={data.ExpiryDate:yyyy-MM-dd}");

                var keyContainer = configAsset != null ? configAsset.KeyContainer : string.Empty;
                var licensePassKey = configAsset != null ? configAsset.LicensePassKey : string.Empty;

                if (string.IsNullOrEmpty(keyContainer) || string.IsNullOrEmpty(licensePassKey))
                    throw new Exception($"null or empty licenseConfig. keyContainer={keyContainer}, passKeyLength={(licensePassKey != null ? licensePassKey.Length : 0)}");

                var licenseData = $"{data.Uuid}|{data.ContentsId}|{CreateLicensePassKey(licensePassKey, data.Uuid)}|{data.ExpiryDate:yyyy-MM-dd}";
                return SignData(licenseData, CreatePrivateKey(keyContainer));
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "GenerateLicense", $"{ex.Message}");
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
                Log(DebugerLogType.Info, "CreatePrivateKey", $"{ex.Message}");
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
                    var signedBytes = rsa.SignData(dataBytes, new SHA256CryptoServiceProvider());
                    var signedData = Convert.ToBase64String(signedBytes);

                    return $"{licenseData}.{signedData}";
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "SignData", $"{ex.Message}");
            }
            return string.Empty;
        }

#endif

        /// <summary>ライセンス読込み ※ここでは日付のみチェックする</summary>
        public static LicenseStatusEnum ValidateLicense(LicenseConfigAsset configAsset, string licenseCode, out LicenseData date)
        {
            date = new LicenseData();
            var status = LicenseStatusEnum.None;

            try
            {
                var keyContainer = configAsset != null ? configAsset.KeyContainer : string.Empty;
                var licensePassKey = configAsset != null ? configAsset.LicensePassKey : string.Empty;

                if (string.IsNullOrEmpty(keyContainer) || string.IsNullOrEmpty(licensePassKey))
                    throw new Exception($"null or empty licenseConfig. keyContainer={keyContainer}, passKeyLength={(licensePassKey != null ? licensePassKey.Length : 0)}");

                var licenseData = VerifySignature(licenseCode, CreatePublicKey(keyContainer));
                var dataParts = licenseData.Split('|');

                if (dataParts.Length == partsLength)
                {
                    if (dataParts[2] == CreateLicensePassKey(licensePassKey, dataParts[0]))
                    {
                        if (DateTime.TryParse(dataParts[3], out date.CreateDate))
                        {
                            date.Uuid = dataParts[0];
                            date.ContentsId = dataParts[1];

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
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "ValidateLicense", $"{ex.Message}");

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
                Log(DebugerLogType.Info, "GetCspParams", $"{ex.Message}");
            }
            return null;
        }

        private static RSAParameters CreatePublicKey(string keyContainer)
        {
            try
            {
                RSAParameters publicKey;

                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(GetCspParams(keyContainer)))
                {
                    publicKey = rsa.ExportParameters(false);
                    return publicKey;
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "CreatePublicKey", $"{ex.Message}");
            }
            return default;
        }

        private static string CreateLicensePassKey(string licensePassKey, string deviceId)
        {
            if (!string.IsNullOrEmpty(deviceId) && !string.IsNullOrEmpty(licensePassKey))
                return deviceId + licensePassKey;

            return string.Empty;
        }

        private static string VerifySignature(string signedData, RSAParameters publicKey)
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
                    rsa.ImportParameters(publicKey);
                    var dataBytes = Encoding.UTF8.GetBytes(licenseData);
                    var isValid = rsa.VerifyData(dataBytes, new SHA256CryptoServiceProvider(), signedBytes);

                    if (!isValid)
                        throw new CryptographicException("signature verification failed");

                    return licenseData;
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "VerifySignature", $"{ex.Message}");
            }
            return string.Empty;
        }
    }
}
