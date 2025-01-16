using System;
using System.IO;
using UnityEngine;

namespace Renard.AssetBundleUniTask
{
    [Serializable]
    public class AssetBundleConfigAsset : ScriptableObject
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "AssetBundleConfig";
        public const string FileExtension = "asset";

        [Header("※必ずResources下に置いてください")]

        [Header("暗号化の有効")]
        [SerializeField] private bool _isEncrypt = false;
        public bool IsEncrypt => _isEncrypt;

        [Header("暗号化キー情報")]
        [SerializeField] private string _encryptKey = "RenardAsset";
        public string EncryptIV => $"iv_{_encryptKey}";
        public string EncryptKey => $"key_{_encryptKey}";

        [Header("ローカル内パス(空でも可)")]
        [SerializeField] private string _outputPath = AssetBundleManager.DefaultOutputPath;
        public string OutputPath => _outputPath;

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
    }

#if UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)

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

#endif
}
