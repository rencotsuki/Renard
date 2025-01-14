using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Renard
{
    public static class ApplicationVersionAssetExtensions
    {
#if UNITY_EDITOR
        public static void Save(this ApplicationVersionAsset target)
        {
            if (target == null) return;

            var fullPath = $"{ApplicationVersionAsset.Path}/{ApplicationVersionAsset.FileName}.{ApplicationVersionAsset.FileExtension}";

            try
            {
                if (!Directory.Exists(ApplicationVersionAsset.Path))
                    Directory.CreateDirectory(ApplicationVersionAsset.Path);

                EditorUtility.SetDirty(target);
                AssetDatabase.CreateAsset(target, fullPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.Log($"{target.GetType().Name}::Save <color=red>error</color>. {ex.Message}\r\npath={fullPath}");
            }
        }
#endif  // UNITY_EDITOR
    }

#if UNITY_EDITOR
    public class ApplicationVersionBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        private static void OnSave()
        {
            var asset = new ApplicationVersionAsset();
            // TODO: Winでも「PlayerSettings.macOS」を使う
            asset?.Set(Application.version, int.Parse(PlayerSettings.macOS.buildNumber));
            asset?.Save();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS ||
                report.summary.platform == BuildTarget.Android)
                return;

            OnSave();
        }

        [MenuItem("Assets/Create/Renard/ApplicationVersion")]
        private static void CreateApplicationVersionAsset() => OnSave();
    }
#endif  // UNITY_EDITOR
}
