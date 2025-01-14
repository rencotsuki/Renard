using UnityEngine;
using UnityEditor;
using Renard.AssetBundleUniTask;

namespace Renard
{
    public static class AssetBundleBuildEditor
    {
        private static bool IsEncrypt => false;

        private static string OutputPath()
        {
            return $"{Application.dataPath}/../../Output";
        }

        private static void OnBuildAssetBundles(BuildTarget target)
        {
            AssetBundleBuildScript.BuildAssetBundles(target, OutputPath(), IsEncrypt);
        }

        [MenuItem("Renard/AssetBundle/SimulationMode", false, 3)]
        public static void ToggleSimulationMode()
        {
            AssetBundleBuildConfig.IsSimulateMode = !AssetBundleBuildConfig.IsSimulateMode;
        }

        [MenuItem("Renard/AssetBundle/SimulationMode", true, 3)]
        public static bool ToggleSimulationModeValidate()
        {
            Menu.SetChecked("Renard/AssetBundle/SimulationMode", AssetBundleBuildConfig.IsSimulateMode);
            return true;
        }

        [MenuItem("Renard/AssetBundle/Build/Win", false)]
        public static void BuildAssetBundlesWin()
        {
#if UNITY_EDITOR_WIN
            // 64ビットなのかを見て作成する
            OnBuildAssetBundles(System.Environment.Is64BitProcess ? BuildTarget.StandaloneWindows64 : BuildTarget.StandaloneWindows);
#else
            Debug.Log($"not build [Win]. target build platform [WIN]. platform={Application.platform}");
#endif
        }

        [MenuItem("Renard/AssetBundle/Build/Android", false)]
        public static void BuildAssetBundlesAndroid()
        {
            OnBuildAssetBundles(BuildTarget.Android);
        }

        [MenuItem("Renard/AssetBundle/Build/OSX", false)]
        public static void BuildAssetBundlesOSX()
        {
#if UNITY_EDITOR_OSX
            OnBuildAssetBundles(BuildTarget.StandaloneOSX);
#else
            Debug.Log($"not build [OSX]. target build platform [OSX]. platform={Application.platform}");
#endif
        }

        [MenuItem("Renard/AssetBundle/Build/iOS", false)]
        public static void BuildAssetBundlesiOS()
        {
#if UNITY_EDITOR_OSX
            OnBuildAssetBundles(BuildTarget.iOS);
#else
            Debug.Log($"not build [iOS]. target build platform [OSX]. platform={Application.platform}");
#endif
        }

        [MenuItem("Renard/AssetBundle/Build/ALL", false)]
        public static void BuildAssetBundlesAllTarget()
        {
            BuildAssetBundlesWin();
            BuildAssetBundlesAndroid();
            BuildAssetBundlesOSX();
            BuildAssetBundlesiOS();
        }
    }
}
