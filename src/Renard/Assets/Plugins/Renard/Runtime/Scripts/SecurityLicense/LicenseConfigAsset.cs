using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renard.License
{
    [Serializable]
    public class LicenseConfigAsset : ScriptableObject
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "LicenseConfig";
        public const string FileExtension = "asset";

        [Header("※必ずResources下に置いてください")]

        [Header("ライセンスファイル拡張子(空でも可)")]
        [SerializeField] private string _licenseFileExtension = "";
        public string LicenseFileExtension => _licenseFileExtension;

        [Header("アプリ識別情報")]
        [SerializeField] private string _contentsId = "Renard";
        public string ContentsId => _contentsId;

        [Header("ライセンス識別キー(空でも可)")]
        [SerializeField] private string _licensePassKey = "";
        public string LicensePassKey => _licensePassKey;

        [Header("Key-32文字 ※変更には注意")]
        [SerializeField] private string _encryptKey = "";
        public string EncryptKey => _encryptKey;

        [Header("IV-16文字 ※変更には注意")]
        [SerializeField] private string _encryptIV = "";
        public string EncryptIV => _encryptIV;

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

        public void Create()
        {
            _contentsId = LicenseManager.GenerateContentsId();
            _licensePassKey = string.Empty;

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
                Debug.Log($"{typeof(LicenseConfigAsset).Name}::CreateEncrypt <color=red>error</color>. {ex.Message}");

                _encryptKey = string.Empty;
                _encryptIV = string.Empty;
            }
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(LicenseConfigAsset))]
    public class LicenseConfigAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var handler = target as LicenseConfigAsset;

            DrawDefaultInspector();

            EditorGUILayout.Space(50);

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

    public static class LicenseConfigEditor
    {
        [MenuItem("Assets/Create/Renard/LicenseConfig")]
        private static void CreateLicenseConfigAsset()
        {
            // １回ロードしてAssetが存在するか確認する
            if (LicenseConfigAsset.Load() != null)
                return;

            var result = new LicenseConfigAsset();
            var fullPath = $"{LicenseConfigAsset.Path}/{LicenseConfigAsset.FileName}.{LicenseConfigAsset.FileExtension}";

            result?.Create();

            try
            {
                if (!Directory.Exists(LicenseConfigAsset.Path))
                    Directory.CreateDirectory(LicenseConfigAsset.Path);

                EditorUtility.SetDirty(result);
                AssetDatabase.CreateAsset(result, fullPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(LicenseConfigEditor).Name}::Save <color=red>error</color>. {ex.Message}\r\npath={fullPath}");
            }
        }
    }

#endif // UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
#endif // UNITY_EDITOR
}
