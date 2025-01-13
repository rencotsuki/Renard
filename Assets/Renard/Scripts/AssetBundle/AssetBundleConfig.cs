using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renard.AssetBundleUniTask
{
    public class AssetBundleConfig
    {
        /// <summary>ログ出力(true=出力する)</summary>
        public static bool IsDebugLog => false;

        /// <summary>ロードタイムアウト時間[s]</summary>
        public static float LoadingTimeOut => 30f;

        /// <summary>ロードのリトライ試行回数</summary>
        public static int LoadRetryCount => 5;

        /// <summary>ディレクトリ名</summary>
        public static string FolderName => "AssetBundles";

        /// <summary>Manifestファイル拡張子</summary>
        public static string ManifestFileExtension => "manifest";

        /// <summary>Hashファイル拡張子</summary>
        public static string HashFileName => "AssetBundleHash";

        /// <summary>Hashファイル拡張子</summary>
        public static string HashFileExtension => "json";

        /// <summary>プラットフォーム対応フォルダ名</summary>
        public static string GetPlatformDirectoryName(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";

                case RuntimePlatform.Android:
                    return "Android";

                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "OSX";

                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";

#if !UNITY_5_4_OR_NEWER
                case RuntimePlatform.OSXWebPlayer:
                case RuntimePlatform.WindowsWebPlayer:
                    return "WebPlayer";
#endif
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";

                default:
                    break;
            }

            return string.Empty;
        }

        /// <summary>Manifestファイル名(プラットフォーム別)</summary>
        public static string GetPlatformManifestName(RuntimePlatform platform)
            => $"{GetPlatformDirectoryName(platform)}";
    }

    public class AssetBundleBuildConfig : AssetBundleConfig
    {
        public static string Encrypt_IV => $"iv_{Application.productName.ToLower()}";
        public static string Encrypt_KEY => $"key_{Application.productName.ToLower()}";

#if UNITY_EDITOR
        protected static int _isSimulateMode = -1;
        protected const string _simulateMode = "SimulateAssetBundles";
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

#if UNITY_EDITOR

        public static RuntimePlatform ToRuntimePlatform(BuildTarget buildPlatform)
        {
            switch (buildPlatform)
            {
                case BuildTarget.iOS:
                    return RuntimePlatform.IPhonePlayer;

                case BuildTarget.Android:
                    return RuntimePlatform.Android;

                case BuildTarget.StandaloneOSX:
                    return RuntimePlatform.OSXPlayer;

#if !UNITY_5_4_OR_NEWER
                case BuildTarget.WebPlayer:
                    return Application.platform;
#endif

                case BuildTarget.WebGL:
                    return RuntimePlatform.WebGLPlayer;

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                default:
                    break;
            }

            return RuntimePlatform.WindowsPlayer;
        }

        /// <summary>プラットフォーム対応フォルダ名</summary>
        public static string GetPlatformDirectoryName(BuildTarget buildPlatform)
            => GetPlatformDirectoryName(ToRuntimePlatform(buildPlatform));

        /// <summary>出力先ディレクトリ(プラットフォーム別)</summary>
        public static string GetPlatformManifestName(BuildTarget buildPlatform)
            => GetPlatformManifestName(ToRuntimePlatform(buildPlatform));
#endif
    }
}