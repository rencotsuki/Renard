using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Renard
{
    public class ApplicationVersionEditor : IPreprocessBuildWithReport
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
}
