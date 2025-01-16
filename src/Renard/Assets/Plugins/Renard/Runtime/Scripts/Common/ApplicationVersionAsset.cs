using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace Renard
{
    [Serializable]
    public class ApplicationVersionAsset : ScriptableObject
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "ApplicationVersion";
        public const string FileExtension = "asset";

        [Header("※必ずResources下に置いてください")]

        [Header("バージョン")]
        [SerializeField] private string _version = "0.0.0";
        public string Version => _version;

        [Header("ビルド")]
        [SerializeField] private int _buildNumber = 0;
        public int BuildNumber => _buildNumber;

        public static ApplicationVersionAsset Load()
        {
            try
            {
                return Resources.Load<ApplicationVersionAsset>(FileName);
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(ApplicationVersionAsset).Name}::Load <color=red>error</color>. {ex.Message}");
                return null;
            }
        }

        public void Set(string version, int buildNumber)
        {
            _version = version;
            _buildNumber = buildNumber;
        }
    }

#if UNITY_EDITOR

    public class ApplicationVersionEditor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            // モバイル端末ではPlayerSetting側をそのまま利用するので作らない
            if (report.summary.platform == BuildTarget.iOS ||
                report.summary.platform == BuildTarget.Android)
                return;

            CreateApplicationVersionAsset();
        }

        [MenuItem("Assets/Create/Renard/ApplicationVersion")]
        private static void CreateApplicationVersionAsset()
        {
            // １回ロードしてAssetが存在するか確認する
            if (ApplicationVersionAsset.Load() != null)
                return;

            var fullPath = $"{ApplicationVersionAsset.Path}/{ApplicationVersionAsset.FileName}.{ApplicationVersionAsset.FileExtension}";

            try
            {

                if (!Directory.Exists(ApplicationVersionAsset.Path))
                    Directory.CreateDirectory(ApplicationVersionAsset.Path);

                var asset = new ApplicationVersionAsset();

                // Winでは「PlayerSettings.macOS」を使う
                asset?.Set(Application.version, int.Parse(PlayerSettings.macOS.buildNumber));

                EditorUtility.SetDirty(asset);
                AssetDatabase.CreateAsset(asset, fullPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(ApplicationVersionEditor)}::Save <color=red>error</color>. {ex.Message}\r\npath={fullPath}");
            }
        }
    }

#endif
}
