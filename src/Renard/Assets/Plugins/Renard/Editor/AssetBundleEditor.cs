using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Renard.AssetBundleUniTask
{
    public static class AssetBundleEditor
    {
        private static AssetBundleConfigAsset assetBundleConfig = null;
        private static string outputPath = string.Empty;

        private static bool LoadAssetBundleConfig()
        {
            try
            {
                assetBundleConfig = AssetBundleConfigAsset.Load();
                if (assetBundleConfig == null)
                    throw new Exception("not found assetBundleConfig.");

                outputPath = assetBundleConfig.OutputFullPath;
                return  true;
            }
            catch
            {
                Debug.Log("<color=yellow>【重要】</color> AssetBundleConfig.assetがResources下に定義されていません");
                return false;
            }
        }

        private static void OnBuildAssetBundles(BuildTarget target)
        {
            try
            {
                if (assetBundleConfig == null)
                    throw new Exception("not found assetBundleConfig.");

                var config = assetBundleConfig.GetConfig(target);

                AssetBundleBuildScript.BuildAssetBundles(target, outputPath, assetBundleConfig, false, true);

                if (assetBundleConfig.GetConfig(target).IsEncrypt)
                {
                    Debug.Log($"build <color=yellow>success</color>. platform={target}, path={outputPath}\n\rpath(encrypt)={outputPath}/{AssetBundleBuildScript.OutputEncryptPath}");
                }
                else
                {
                    Debug.Log($"build <color=yellow>success</color>. platform={target}, path={outputPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"OnBuildAssetBundles: error - {ex.Message}");
            }
        }

        [MenuItem("Renard/AssetBundle/SimulationMode", false, 3)]
        public static void ToggleSimulationMode()
        {
            AssetBundleManager.IsSimulateMode = !AssetBundleManager.IsSimulateMode;
        }

        [MenuItem("Renard/AssetBundle/SimulationMode", true, 3)]
        public static bool ToggleSimulationModeValidate()
        {
            Menu.SetChecked("Renard/AssetBundle/SimulationMode", AssetBundleManager.IsSimulateMode);
            return true;
        }

        [MenuItem("Renard/AssetBundle/Build/Win", false)]
        public static void BuildAssetBundlesWin()
        {
            if (!LoadAssetBundleConfig())
                return;

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
            if (!LoadAssetBundleConfig())
                return;

            OnBuildAssetBundles(BuildTarget.Android);
        }

        [MenuItem("Renard/AssetBundle/Build/OSX", false)]
        public static void BuildAssetBundlesOSX()
        {
            if (!LoadAssetBundleConfig())
                return;

#if UNITY_EDITOR_OSX
            OnBuildAssetBundles(BuildTarget.StandaloneOSX);
#else
            Debug.Log($"not build [OSX]. target build platform [OSX]. platform={Application.platform}");
#endif
        }

        [MenuItem("Renard/AssetBundle/Build/iOS", false)]
        public static void BuildAssetBundlesiOS()
        {
            if (!LoadAssetBundleConfig())
                return;

#if UNITY_EDITOR_OSX
            OnBuildAssetBundles(BuildTarget.iOS);
#else
            Debug.Log($"not build [iOS]. target build platform [OSX]. platform={Application.platform}");
#endif
        }

        [MenuItem("Renard/AssetBundle/Build/ALL", false)]
        public static void BuildAssetBundlesAllTarget()
        {
            if (!LoadAssetBundleConfig())
                return;

            BuildAssetBundlesWin();
            BuildAssetBundlesAndroid();
            BuildAssetBundlesOSX();
            BuildAssetBundlesiOS();
        }

        [MenuItem("Renard/AssetBundle/Copy Assets", false)]
        public static void CopyAssets()
        {
            if (AssetBundleBuildScript.CopyAssets(EditorUserBuildSettings.activeBuildTarget, false, true))
            {
                Debug.Log($"AssetBundles複製:<color=yellow>成功</color>");
            }
            else
            {
                Debug.Log($"AssetBundles複製:<color=red>失敗</color>");
            }
        }
    }
}
