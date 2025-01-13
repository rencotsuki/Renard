using UnityEngine;

namespace Renard
{
    using AssetBundleUniTask;

    public static class AssetBundleBuildEditor
    {
        private static bool IsEncrypt => false;

        private static string OutputPath => $"{Application.dataPath}/../../Output";

        private static void OnBuildAssetBundles(UnityEditor.BuildTarget target)
        {
            AssetBundleBuildScript.BuildAssetBundles(target, OutputPath, IsEncrypt);
        }

        [UnityEditor.MenuItem("Renard/AssetBundle/SimulationMode", false, 3)]
        public static void ToggleSimulationMode()
        {
            AssetBundleBuildConfig.IsSimulateMode = !AssetBundleBuildConfig.IsSimulateMode;
        }

        [UnityEditor.MenuItem("Renard/AssetBundle/SimulationMode", true, 3)]
        public static bool ToggleSimulationModeValidate()
        {
            UnityEditor.Menu.SetChecked("Renard/AssetBundle/SimulationMode", AssetBundleBuildConfig.IsSimulateMode);
            return true;
        }

#if UNITY_EDITOR_WIN

        [UnityEditor.MenuItem("Renard/AssetBundle/Build/Win", false)]
        public static void BuildAssetBundlesWin()
        {
            // 64ビットなのかを見て作成する
            OnBuildAssetBundles(System.Environment.Is64BitProcess ? UnityEditor.BuildTarget.StandaloneWindows64 : UnityEditor.BuildTarget.StandaloneWindows);
        }

        [UnityEditor.MenuItem("Renard/AssetBundle/Build/Android", false)]
        public static void BuildAssetBundlesAndroid()
        {
            OnBuildAssetBundles(UnityEditor.BuildTarget.Android);
        }

        [UnityEditor.MenuItem("Renard/AssetBundle/Build/ALL", false)]
        public static void BuildAssetBundlesAllTarget()
        {
            BuildAssetBundlesWin();
            BuildAssetBundlesAndroid();
        }

#elif UNITY_EDITOR_OSX

        [UnityEditor.MenuItem("Renard/AssetBundle/Build/OSX", false)]
        public static void BuildAssetBundlesOSX()
        {
            OnBuildAssetBundles(UnityEditor.BuildTarget.StandaloneOSX);
        }
        
        [UnityEditor.MenuItem("Renard/AssetBundle/Build/iOS", false)]
        public static void BuildAssetBundlesiOS()
        {
            OnBuildAssetBundles(UnityEditor.BuildTarget.iOS);
        }
        
        [UnityEditor.MenuItem("Renard/AssetBundle/Build/ALL", false)]
        public static void BuildAssetBundlesAllTarget()
        {
            BuildAssetBundlesOSX();
            BuildAssetBundlesiOS();
        }

#endif  // UNITY_EDITOR_WIN, UNITY_EDITOR_OSX
    }
}
