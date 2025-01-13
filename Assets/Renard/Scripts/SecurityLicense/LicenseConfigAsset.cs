using System;
using System.IO;
using UnityEngine;

namespace Renard.License
{
    [Serializable]
    public class LicenseConfigAsset : ScriptableObject
    {
        public const string Path = "Assets/Renard/Resources";
        public const string FileName = "LicenseConfig";
        public const string FileExtension = "asset";

        [Header("※16桁のキー(ハイフンも含む)")]
        [SerializeField] private string _encryptKey = "AAAAA-BBBBB-1234";
        public string EncryptKey => _encryptKey;

        [Header("※アプリ内での識別情報(空でも可)")]
        [SerializeField] private string _contentsId = "Renard-001A";
        public string ContentsId => _contentsId;

        [Header("※基本は{プロダクト名}KeyContainer")]
        [SerializeField] private string _keyContainer = "RenardKeyContainer";
        public string KeyContainer => _keyContainer;

        [Header("※プロダクトごとに決める")]
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
                Debug.Log($"LicenseConfigAsset::Load <color=red>error</color>. {ex.Message}");
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
