using System;
using System.IO;
using UnityEngine;

namespace Renard.Sample
{
    [Serializable]
    public class LauncherConfigData
    {
        public int TargetFrameRate = LauncherConfig.DefaultTargetFrameRate;
        public string FirstSceneName = LauncherConfig.DefaultFirstSceneName;
        public string[] additiveScenes = LauncherConfig.DefaultAdditiveScenes;
    }

    [Serializable]
    public class LauncherConfig : ScriptableObject
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "LauncherConfig";
        public const string FileExtension = "asset";

        public const int DefaultTargetFrameRate = 60;
        public const string DefaultFirstSceneName = "Sample";
        public static string[] DefaultAdditiveScenes => new string[0];

        [SerializeField] private string copyrightFirstYear = "2025";
        [SerializeField] private string minAppVersion = string.Empty;
        [SerializeField] private int minBuildNumber = 0;
        [SerializeField] private LauncherConfigData windows = new LauncherConfigData();
        [SerializeField] private LauncherConfigData osx = new LauncherConfigData();
        [SerializeField] private LauncherConfigData ios = new LauncherConfigData();
        [SerializeField] private LauncherConfigData android = new LauncherConfigData();
        [SerializeField] private LauncherConfigData other = new LauncherConfigData();

        public static LauncherConfig Load()
        {
            return Resources.Load<LauncherConfig>(FileName);
        }

        public LauncherConfigData GetConfig()
        {
            ApplicationCopyright.FirstYear = copyrightFirstYear;
            ApplicationVersion.MinVersion = minAppVersion;
            ApplicationVersion.MinBuildVersion = minBuildNumber;

#if UNITY_EDITOR
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows ||
                UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneWindows64)
            {
                return windows;
            }

            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneOSX)
            {
                return osx;
            }

            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            {
                return ios;
            }

            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
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
        [UnityEditor.MenuItem("Assets/Create/Renard/LauncherConfig")]
        private static void CreateLicenseConfigAsset()
        {
            var result = new LauncherConfig();
            var fullPath = $"{LauncherConfig.Path}/{LauncherConfig.FileName}.{LauncherConfig.FileExtension}";

            try
            {
                if (!Directory.Exists(LauncherConfig.Path))
                    Directory.CreateDirectory(LauncherConfig.Path);

                UnityEditor.EditorUtility.SetDirty(result);
                UnityEditor.AssetDatabase.CreateAsset(result, fullPath);

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(LauncherConfigEditor).Name}::Save <color=red>error</color>. {ex.Message}\r\npath={fullPath}");
            }
        }
    }

#endif
}