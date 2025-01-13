using System;
using UnityEngine;

namespace Renard
{
    [Serializable]
    public class DeviceUuidHandler : MonoBehaviourCustom
    {
        public static string Uuid { get; private set; } = string.Empty;

        protected string ProductKey => $"com.{Application.companyName}.{Application.productName}";

        private void Awake()
        {
            isDebugLog = false;

            /*
             * iOSではdeviceUniqueIdentifierが
             * Unityバージョン、OSバージョンで
             * 可変してしまうので絶対に使わない！
             */

#if UNITY_IOS && !UNITY_EDITOR
            Uuid = GetUuid(ProductKey);
#else
            Uuid = SystemInfo.deviceUniqueIdentifier;
#endif
        }

        public void Clear()
        {
#if UNITY_IOS && !UNITY_EDITOR
            DeleteUuid(ProductKey);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern IntPtr getPersistentUUID(string key);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void deletePersistentUUID(string key);

        private string GetUuid(string key)
        {
            try
            {
                if (Application.platform != RuntimePlatform.IPhonePlayer)
                    throw new Exception("Not Supported on this platform");

                IntPtr uuidPtr = getPersistentUUID(key);
                return System.Runtime.InteropServices.Marshal.PtrToStringAuto(uuidPtr);
            }
            catch (Exception ex)
            {
                Log( DebugerLogType.Info, "GetUUID", $"{ex.Message}");
                return string.Empty;
            }
        }

        private void DeleteUuid(string key)
        {
            try
            {
                if (Application.platform != RuntimePlatform.IPhonePlayer)
                    throw new Exception("Not Supported on this platform");

                deletePersistentUUID(key);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "DeleteUUID", $"{ex.Message}");
            }
        }

#endif
    }
}
