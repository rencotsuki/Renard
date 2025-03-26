using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renard.Sample
{
    [Serializable]
    public class LauncherConfigData
    {
        public int TargetFrameRate = LauncherConfigAsset.DefaultTargetFrameRate;
        public bool SkipLicense = false;
        public string LicenseLocalPath = LicenseHandler.DefaultLocalPath;
        public string FirstSceneName = LauncherConfigAsset.DefaultFirstSceneName;
        public string[] additiveScenes = LauncherConfigAsset.DefaultAdditiveScenes;
    }

    [Serializable]
    public class LauncherConfigAsset : ScriptableObject
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "LauncherConfig";
        public const string FileExtension = "asset";

        public const int DefaultTargetFrameRate = 60;
        public const string DefaultFirstSceneName = "Sample";
        public static string[] DefaultAdditiveScenes => new string[0];

        [Header("※必ずResources下に置いてください")]

        [Header("ライセンス年度")]
        [SerializeField] private string copyrightFirstYear = "2025";
        [Header("アプリケーション連携時の動作最低バージョン")]
        [SerializeField] private string minAppVersion = string.Empty;
        [Header("アプリケーション連携時の動作最低ビルド")]
        [SerializeField] private int minBuildNumber = 0;

        [Header("各プラットフォーム設定")]
        [SerializeField] private LauncherConfigData windows = new LauncherConfigData();
        [SerializeField] private LauncherConfigData osx = new LauncherConfigData();
        [SerializeField] private LauncherConfigData ios = new LauncherConfigData();
        [SerializeField] private LauncherConfigData android = new LauncherConfigData();
        [SerializeField] private LauncherConfigData other = new LauncherConfigData();

        public static LauncherConfigAsset Load()
        {
            return Resources.Load<LauncherConfigAsset>(FileName);
        }

        public LauncherConfigData GetConfig()
        {
            ApplicationCopyright.FirstYear = copyrightFirstYear;
            ApplicationVersion.MinVersion = minAppVersion;
            ApplicationVersion.MinBuildVersion = minBuildNumber;

#if UNITY_EDITOR
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
            {
                return windows;
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX)
            {
                return osx;
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                return ios;
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                return android;
            }
#else
            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                return windows;
            }

            if (Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                return osx;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return ios;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                return android;
            }
#endif
            return other;
        }
    }

#if UNITY_EDITOR

    public static class LauncherConfigEditor
    {
        [MenuItem("Assets/Create/Renard/LauncherConfig")]
        private static void CreateLicenseConfigAsset()
        {
            // １回ロードしてAssetが存在するか確認する
            if (LauncherConfigAsset.Load() != null)
                return;

            var result = new LauncherConfigAsset();
            var fullPath = $"{LauncherConfigAsset.Path}/{LauncherConfigAsset.FileName}.{LauncherConfigAsset.FileExtension}";

            try
            {
                if (!Directory.Exists(LauncherConfigAsset.Path))
                    Directory.CreateDirectory(LauncherConfigAsset.Path);

                EditorUtility.SetDirty(result);
                AssetDatabase.CreateAsset(result, fullPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(LauncherConfigEditor).Name}::Save <color=red>error</color>. {ex.Message}\r\npath={fullPath}");
            }
        }
    }

#endif
}