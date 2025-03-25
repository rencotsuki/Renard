using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif // UNITY_IOS && !UNITY_EDITOR

namespace Renard
{
    public class ApplicationVersion
    {
        public static string Version = "0.0.0";
        public static int BuildVersion = 0;

        public static string MinVersion = string.Empty;
        public static int MinBuildVersion = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GetVersion()
        {
            GetAppVersionName(out Version, out BuildVersion);
        }

        private enum versionMoji { Major, Minor, Revision };

        private static int OnGetVersion(string version, versionMoji ver)
        {
            var split = version.Split('.');
            switch (ver)
            {
                case ApplicationVersion.versionMoji.Major:
                    if (split.Length >= 3) return int.Parse(split[0]);
                    break;

                case ApplicationVersion.versionMoji.Minor:
                    if (split.Length >= 2) return int.Parse(split[1]);
                    break;

                case ApplicationVersion.versionMoji.Revision:
                    if (split.Length >= 1) return int.Parse(split[(split.Length - 1)]);
                    break;

                default:
                    break;
            }
            return 0;
        }

        public static int GetVersionMajor(string version) => OnGetVersion(version, versionMoji.Major);
        public static int GetVersionMinor(string version) => OnGetVersion(version, versionMoji.Minor);
        public static int GetVersionRevision(string version) => OnGetVersion(version, versionMoji.Revision);

        private static void GetAppVersionName(out string outVersion, out int outBuildVersion)
        {
#if UNITY_EDITOR
            outVersion = Application.version;
            outBuildVersion = int.Parse(UnityEditor.PlayerSettings.macOS.buildNumber);
#else

#if UNITY_ANDROID
            var pInfo = GetPackageInfo();
            outVersion = pInfo != null ? pInfo.Get<string>("versionName") : "0.0.0";
            outBuildVersion = pInfo != null ? pInfo.Get<int>("versionCode") : 0;
#elif UNITY_IOS
            outVersion = GetVersionName_();
            outBuildVersion = int.Parse(GetBuildVersionName_());
#else
            var asset = ApplicationVersionAsset.Load();
            outVersion = asset != null ? asset.Version : "0.0.0";
            outBuildVersion = asset != null ? asset.BuildNumber : 0;
#endif // UNITY_ANDROID || UNITY_IOS

#endif // UNITY_EDITOR
        }

#if !UNITY_EDITOR

#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern string GetVersionName_();
        [DllImport("__Internal")]
        private static extern string GetBuildVersionName_();
#endif // UNITY_IOS

#if UNITY_ANDROID
        private static AndroidJavaObject GetPackageInfo()
        {
            try
            {            
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                if (unityPlayer == null)
                {
                    return null;
                }
            
                var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getApplicationContext");
                if (context == null)
                {
                    return null;
                }
            
                var pManager = context.Call<AndroidJavaObject>("getPackageManager");
                if (pManager == null)
                {
                    return null;
                }
            
                var pInfo = pManager.Call<AndroidJavaObject>("getPackageInfo", context.Call<string>("getPackageName"), 0);
                return pInfo;
            }
            catch (System.Exception error)
            {
                Debug.Log(error.Message);
                return null;
            }
        }
#endif // UNITY_ANDROID

#endif // !UNITY_EDITOR
    }
}
