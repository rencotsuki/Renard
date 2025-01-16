using System;
using UnityEngine;
using UnityEditor;

namespace Renard.AssetBundleUniTask
{
    public static class AssetBundleEditor
    {
#if UNITY_EDITOR
        private static int _isSimulateMode = -1;
        private const string _simulateMode = "SimulateAssetBundles";
#endif
        public static bool IsSimulateMode
        {
            get
            {
#if UNITY_EDITOR
                if (_isSimulateMode == -1)
                    _isSimulateMode = EditorPrefs.GetBool(_simulateMode, true) ? 1 : 0;
                return _isSimulateMode != 0;
#else
                return false;
#endif
            }
            set
            {
#if UNITY_EDITOR
                int newValue = value ? 1 : 0;
                if (newValue != _isSimulateMode)
                {
                    _isSimulateMode = newValue;
                    EditorPrefs.SetBool(_simulateMode, value);
                }
#endif
            }
        }

        private static void OnBuildAssetBundles(BuildTarget target)
        {
            try
            {
                var assetBundleConfig = AssetBundleConfigAsset.Load();

                var outputPath = string.IsNullOrEmpty(assetBundleConfig.OutputPath) ?
                                    $"{Application.dataPath}/../../{AssetBundleManager.DefaultOutputPath}" :
                                     $"{Application.dataPath}/../../{assetBundleConfig.OutputPath}";

                AssetBundleBuildScript.BuildAssetBundles(target, outputPath, assetBundleConfig.IsEncrypt, assetBundleConfig.EncryptKey);

                Debug.Log($"build <color=yellow>success</color>. platform={target}, path={outputPath}");
            }
            catch (Exception ex)
            {
                Debug.Log($"OnBuildAssetBundles: error - {ex.Message}");
            }
        }

        [MenuItem("Renard/AssetBundle/SimulationMode", false, 3)]
        public static void ToggleSimulationMode()
        {
            IsSimulateMode = !IsSimulateMode;
        }

        [MenuItem("Renard/AssetBundle/SimulationMode", true, 3)]
        public static bool ToggleSimulationModeValidate()
        {
            Menu.SetChecked("Renard/AssetBundle/SimulationMode", IsSimulateMode);
            return true;
        }

        [MenuItem("Renard/AssetBundle/Build/Win", false)]
        public static void BuildAssetBundlesWin()
        {
#if UNITY_EDITOR_WIN
            // 64ビットなのかを見て作成する
            OnBuildAssetBundles(Environment.Is64BitProcess ? BuildTarget.StandaloneWindows64 : BuildTarget.StandaloneWindows);
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
