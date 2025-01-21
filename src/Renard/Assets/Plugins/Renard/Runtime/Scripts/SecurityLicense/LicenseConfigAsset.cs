using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
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
        public const int EncryptKeyLength = 32;
        public const int EncryptIVLength = 16;

        [Header("※必ずResources下に置いてください")]

        [Header("アプリ識別情報")]
        [SerializeField] private string _contentsId = "Renard";
        public string ContentsId => _contentsId;

        [Header("ライセンス識別キー(空でも可)")]
        [SerializeField] private string _licensePassKey = "";
        public string LicensePassKey => _licensePassKey;

        [Header("KeyContainer ※変更には注意")]
        [SerializeField] private string _keyContainer = "RenardKeyContainer";
        public string KeyContainer => _keyContainer;

        [Header("Key-32文字(英数字:a-z,A-z,0-1) ※変更には注意")]
        [SerializeField] private string _encryptKey = "";
        public string EncryptKey => _encryptKey;

        [Header("IV-16文字(英数字:a-z,A-z,0-1) ※変更には注意")]
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

        public void Setup(string productName)
        {
            _contentsId = $"{productName}";
            _licensePassKey = string.Empty;
            _keyContainer = PaddingBase64Key($"{productName}KeyContainer");

            CreateEncrypt();
        }

        private string PaddingBase64Key(string keyContainer)
        {
            try
            {
                var splits = keyContainer.Split('.');
                // 文字数が4の倍数になるように変換
                for (int i = 0; i < (splits[1].Length % 4); i++)
                {
                    splits[1] += "=";
                }
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(splits[1]));
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(LicenseConfigAsset).Name}::PaddingBase64Key <color=red>error</color>. {ex.Message}");
            }
            return string.Empty;
        }

        private const string passChars = @"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+-=";

        private string GeneratePassKey(int length, int seed)
        {
            try
            {
                if (length <= 0)
                    throw new Exception("length zero.");

                var result = new StringBuilder(length);
                var random = new System.Random(seed);
                var index = -1;

                for (int i = 0; i < length; i++)
                {
                    index = random.Next(0, passChars.Length);

                    if (index < 0 || passChars.Length <= index)
                        throw new Exception($"not index. length={passChars.Length}, index={index}");

                    result.Append(passChars[index]);
                }
                return result.ToString();
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(LicenseConfigAsset).Name}::GeneratePassKey <color=red>error</color>. {ex.Message}");
            }
            return string.Empty;
        }

        public void CreateEncrypt()
        {
            try
            {
                var random = new System.Random(DateTime.UtcNow.Millisecond);

                _encryptKey = GeneratePassKey(EncryptKeyLength, random.Next(0, 1000));
                _encryptIV = GeneratePassKey(EncryptIVLength, random.Next(0, 1000));
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

            EditorGUILayout.Space();

            if (GUILayout.Button("Create EncryptKey/IV"))
            {
                handler.CreateEncrypt();
            }
        }
    }

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

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

            result?.Setup(Application.productName);

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

#endif // UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
#endif // UNITY_EDITOR
}
