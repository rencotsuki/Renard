using System;
using System.IO;
using UnityEngine;

namespace Renard.License
{
    [Serializable]
    public class LicenseConfigAsset : ScriptableObject
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "LicenseConfig";
        public const string FileExtension = "asset";
        public const int EncryptKeyLength = 32;
        public const int EncryptIVLength = 16;

        [Header("※必ずResources下に置いてください")]

        [Header("32桁のキー(英数字:a-z,A-z,0-1)")]
        [SerializeField] private string _encryptKey = "0123456789abcdef0123456789abcdef";
        public string EncryptKey => _encryptKey;

        [Header("16桁のキー(英数字:a-z,A-z,0-1)")]
        [SerializeField] private string _encryptIV = "abcdef0123456789";
        public string EncryptIV => _encryptIV;

        [Header("アプリ内での識別情報(空でも可)")]
        [SerializeField] private string _contentsId = "Renard-001A";
        public string ContentsId => _contentsId;

        [Header("基本は{プロダクト名}KeyContainer")]
        [SerializeField] private string _keyContainer = "RenardKeyContainer";
        public string KeyContainer => _keyContainer;

        [Header("プロダクトごとに決める")]
        [SerializeField] private string _licensePassKey = "12345";
        public string LicensePassKey => _licensePassKey;

        public static LicenseConfigAsset Load()
        {
            try
            {
                return Resources.Load<LicenseConfigAsset>(FileName);
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(LicenseConfigAsset).Name}::Load <color=red>error</color>. {ex.Message}");
                return null;
            }
        }
    }

#if UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)

    public static class LicenseConfigEditor
    {
        [UnityEditor.MenuItem("Assets/Create/Renard/LicenseConfig")]
        private static void CreateLicenseConfigAsset()
        {
            // １回ロードしてAssetが存在するか確認する
            if (LicenseConfigAsset.Load() != null)
                return;

            var result = new LicenseConfigAsset();
            var fullPath = $"{LicenseConfigAsset.Path}/{LicenseConfigAsset.FileName}.{LicenseConfigAsset.FileExtension}";

            try
            {
                if (!Directory.Exists(LicenseConfigAsset.Path))
                    Directory.CreateDirectory(LicenseConfigAsset.Path);

                UnityEditor.EditorUtility.SetDirty(result);
                UnityEditor.AssetDatabase.CreateAsset(result, fullPath);

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(LicenseConfigEditor).Name}::Save <color=red>error</color>. {ex.Message}\r\npath={fullPath}");
            }
        }
    }

#endif
}
