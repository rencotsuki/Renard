using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renard.AssetBundleUniTask
{
    [Serializable]
    public class AssetBundleConfigData
    {
        [Header("ローカルパス(空白はデフォルト設定となる)")]
        [SerializeField] private string _localPath = AssetBundleManager.DefaultLocalPath;
        public string LocalPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_localPath))
                    return _localPath;
                return AssetBundleManager.DefaultLocalPath;
            }
        }

        [Header("暗号化の有効")]
        [SerializeField] private bool _isEncrypt = false;
        public bool IsEncrypt => _isEncrypt;
    }

    [Serializable]
    public class AssetBundleConfigAsset : ScriptableObject
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "AssetBundleConfig";
        public const string FileExtension = "asset";

        [Header("※必ずResources下に置いてください")]

        [Header("出力先パス ※フルパスまたはAssetからのカレントパス")]
        [SerializeField] private string _outputPath = "";
        public string OutputPath => _outputPath;
        public string OutputFullPath
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(_outputPath))
                        throw new Exception("null or empty outputPath.");

                    if (System.IO.Path.IsPathRooted(_outputPath))
                        return _outputPath;

                    return $"{Application.dataPath}/{_outputPath}";
                }
                catch (Exception ex)
                {
                    Debug.Log($"{typeof(AssetBundleConfigAsset).Name}::OutputFullPath <color=red>error</color>. {ex.Message}");
                    return string.Empty;
                }
            }
        }

        [Header("暗号化のキー情報")]
        [SerializeField] private string _encryptKey = "";
        public string EncryptKey => _encryptKey;
        [SerializeField] private string _encryptIV = "";
        public string EncryptIV => _encryptIV;

        [Header("各プラットフォーム設定")]
        [SerializeField] private AssetBundleConfigData windows = new AssetBundleConfigData();
        [SerializeField] private AssetBundleConfigData osx = new AssetBundleConfigData();
        [SerializeField] private AssetBundleConfigData ios = new AssetBundleConfigData();
        [SerializeField] private AssetBundleConfigData android = new AssetBundleConfigData();
        [SerializeField] private AssetBundleConfigData other = new AssetBundleConfigData();

        public static AssetBundleConfigAsset Load()
        {
            try
            {
                return Resources.Load<AssetBundleConfigAsset>(FileName);
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(AssetBundleConfigAsset).Name}::Load <color=red>error</color>. {ex.Message}");
                return null;
            }
        }

        public AssetBundleConfigData GetConfig() => GetConfig(Application.platform);

        public AssetBundleConfigData GetConfig(RuntimePlatform platform)
        {
            if (platform == RuntimePlatform.WindowsEditor ||
                platform == RuntimePlatform.WindowsPlayer)
            {
                return windows;
            }

            if (platform == RuntimePlatform.OSXEditor ||
                platform == RuntimePlatform.OSXPlayer)
            {
                return osx;
            }

            if (platform == RuntimePlatform.IPhonePlayer)
            {
                return ios;
            }

            if (platform == RuntimePlatform.Android)
            {
                return android;
            }
            return other;
        }

#if UNITY_EDITOR
        public AssetBundleConfigData GetConfig(BuildTarget buildTarget)
        {
            if (buildTarget == BuildTarget.StandaloneWindows ||
                buildTarget == BuildTarget.StandaloneWindows64)
            {
                return windows;
            }

            if (buildTarget == BuildTarget.StandaloneOSX)
            {
                return osx;
            }

            if (buildTarget == BuildTarget.iOS)
            {
                return ios;
            }

            if (buildTarget == BuildTarget.Android)
            {
                return android;
            }
            return other;
        }
#endif

        public void Create()
        {
            _outputPath = $"../../CreateAssetBundles";

            CreateEncrypt();
        }

        public void CreateEncrypt()
        {
            try
            {
                var random = new System.Random(DateTime.UtcNow.Millisecond);

                _encryptKey = AESGenerator.GenerateKey(AESGenerator.EncryptKeyLength, random.Next(1, 1000), Debug.isDebugBuild);
                _encryptIV = AESGenerator.GenerateKey(AESGenerator.EncryptIVLength, random.Next(1, 1000), Debug.isDebugBuild);
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(AssetBundleConfigAsset).Name}::CreateEncrypt <color=red>error</color>. {ex.Message}");

                _encryptKey = string.Empty;
                _encryptIV = string.Empty;
            }
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(AssetBundleConfigAsset))]
    public class AssetBundleConfigAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var handler = target as AssetBundleConfigAsset;

            DrawDefaultInspector();

            EditorGUILayout.Space(25);

            var helpMessage = "ローカルパスは、\n\r";
            helpMessage += "AssetBundleManager.SetupAsyncの引数で\n\r";
            helpMessage += "指定したパスからのカレントパスとなります。\n\r\n\r";
            helpMessage += "未指定(空文字指定)の場合は、\n\r";
            helpMessage += "Application.persistentDataPathからのカレントパスとなります。";

            EditorGUILayout.HelpBox(helpMessage, MessageType.Info);

            EditorGUILayout.Space(25);

            if (GUILayout.Button("Create Keys"))
            {
                var message = "KeyContainer, EncryptKey, EncryptIV\n\r\n\r更新すると元に戻せません。\n\r更新しますか？";
                if (EditorUtility.DisplayDialog("Create Keys", message, "OK", "Cancel"))
                {
                    handler.CreateEncrypt();
                }
            }
        }
    }
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

    public static class AssetBundleConfigEditor
    {
        [UnityEditor.MenuItem("Assets/Create/Renard/AssetBundleConfig")]
        private static void CreateAssetBundleConfigAsset()
        {
            // １回ロードしてAssetが存在するか確認する
            if (AssetBundleConfigAsset.Load() != null)
                return;

            var result = new AssetBundleConfigAsset();
            var fullPath = $"{AssetBundleConfigAsset.Path}/{AssetBundleConfigAsset.FileName}.{AssetBundleConfigAsset.FileExtension}";

            result?.Create();

            try
            {
                if (!Directory.Exists(AssetBundleConfigAsset.Path))
                    Directory.CreateDirectory(AssetBundleConfigAsset.Path);

                UnityEditor.EditorUtility.SetDirty(result);
                UnityEditor.AssetDatabase.CreateAsset(result, fullPath);

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(AssetBundleConfigEditor).Name}::Save <color=red>error</color>. {ex.Message}\r\npath={fullPath}");
            }
        }
    }

#endif // UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
#endif // UNITY_EDITOR
}
