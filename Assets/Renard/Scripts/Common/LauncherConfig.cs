using System;
using UnityEngine;

namespace Renard
{
    [Serializable]
    public class LauncherConfigData
    {
        public int TargetFrameRate = LauncherConfig.DefaultTargetFrameRate;
        public string FirstSceneName = LauncherConfig.DefaultFirstSceneName;
        public string[] additiveScenes = LauncherConfig.DefaultAdditiveScenes;
    }

    [Serializable]
    [CreateAssetMenu(fileName = "LauncherConfig", menuName = "Renard/LauncherConfig")]
    public class LauncherConfig : ScriptableObject
    {
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
            return Resources.Load<LauncherConfig>("LauncherConfig");
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
}