using System;
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

    public sealed class LicenseManager
    {
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

        /// <summary></summary>
        public static string GenerateContentsId() => Application.productName;

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

                    if (date.ValidityDays > 0 && date.ExpiryDate > LicenseHandler.GetNow())
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
