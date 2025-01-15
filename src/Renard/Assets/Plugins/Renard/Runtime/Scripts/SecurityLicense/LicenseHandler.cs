using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor;
#endif

namespace Renard
{
    using License;

    public class LicenseHandler : MonoBehaviourCustom
    {
        protected string fileName => $"License-{Application.productName}";
        protected string fileExtension => ""; //拡張子を付けられるようにしておく
        protected string fileFullName => $"{(string.IsNullOrEmpty(fileExtension) ? fileName : $"{fileName}.{fileExtension}")}";

        public string OutputFilePath
        {
            get
            {
#if UNITY_EDITOR
                return $"{Application.dataPath}/../../License_Output/{fileFullName}";
#else
                return $"{Application.dataPath}/../License_Output/{fileFullName}";
#endif
            }
        }

        public string ActivationFilePath => $"{Application.persistentDataPath}/{fileFullName}";

        public LicenseStatusEnum Status { get; protected set; } = LicenseStatusEnum.None;

        public static int[] ValidityDaysList => new int[] { 7, 14, 21, 30, 60, 120, 180, 210, 240, 270, 300, 330, 365 };

        protected LicenseData licenseData = default;
        public string Uuid => licenseData.Uuid;
        public string ContentsId => licenseData.ContentsId;
        public string ExpiryDate => $"{licenseData.ExpiryDate:yyyy-MM-dd}";

        private LicenseConfigAsset _licenseConfig = null;
        protected LicenseConfigAsset licenseConfig
        {
            get
            {
                try
                {
                    if (_licenseConfig == null)
                        _licenseConfig = LicenseConfigAsset.Load();
                }
                catch (Exception ex)
                {
                    Log(DebugerLogType.Info, "licenseConfig", $"{ex.Message}");
                    _licenseConfig = null;
                }
                return _licenseConfig;
            }
        }
        protected string m_EncryptKey
        {
            get
            {
                if (licenseConfig != null)
                    return licenseConfig.EncryptKey;
                return string.Empty;
            }
        }
        public string m_ContentsId
        {
            get
            {
                if (licenseConfig != null)
                    return licenseConfig.ContentsId;
                return string.Empty;
            }
        }

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

        /*
         * 配布側のアプリでは動作させない
         */

        /// <summary>ライセンスファイル生成</summary>
        public bool Create(LicenseData data)
        {
            try
            {
                if (licenseConfig == null)
                {
                    Debug.Log("<color=yellow>【重要】</color> LicenseConfig.assetが定義されていません。");
                    throw new Exception("not found licenseConfig.");
                }

                // ライセンスコードを生成
                var licenseCode = LicenseManager.GenerateLicense(licenseConfig, data);
                return OnEncryptAndSaveToFile(licenseCode, OutputFilePath);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "Encrypt", $"{ex.Message}");
            }
            return false;
        }

        // 暗号化して保存
        protected bool OnEncryptAndSaveToFile(string licenseCode, string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(m_EncryptKey))
                    throw new Exception("null or empty encryptKey.");

                if (string.IsNullOrEmpty(licenseCode))
                    throw new Exception("null or empty licenseCode.");

                if (string.IsNullOrEmpty(filePath))
                    throw new Exception("null or empty filePath.");

                if (!File.Exists(filePath))
                    throw new Exception($"not found filePath. path={filePath}");

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(m_EncryptKey);
                    aesAlg.IV = Encoding.UTF8.GetBytes(m_EncryptKey);

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    using (CryptoStream cs = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(licenseCode);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnEncryptAndSaveToFile", $"{ex.Message}");
            }
            return false;
        }

#endif

        /// <summary>ライセンス確認</summary>
        public LicenseStatusEnum Activation(string uuid)
        {
            Status = LicenseStatusEnum.None;
            licenseData = default;

            try
            {
                if (licenseConfig == null)
                {
                    Debug.Log("<color=yellow>【重要】</color> LicenseConfig.assetが定義されていません。");
                    throw new Exception("not found licenseConfig.");
                }

                // ライセンスコードを読込み
                var licenseCode = OnDecryptFromFile(ActivationFilePath);
                if (licenseCode == null || licenseCode.Length <= 0)
                {
                    Status = LicenseStatusEnum.NotFile;
                    throw new Exception($"not found file. path={ActivationFilePath}");
                }

                Status = LicenseManager.ValidateLicense(licenseConfig, licenseCode, out licenseData);

                // ライセンスデータを確認する
                if (Status == LicenseStatusEnum.Success)
                {
                    // uuidをチェック
                    if (string.IsNullOrEmpty(uuid) || uuid != licenseData.Uuid)
                    {
                        Status = LicenseStatusEnum.Injustice;
                        throw new Exception($"injustice license. uuid error.");
                    }

                    // contentsIdをチェック
                    if (string.IsNullOrEmpty(m_ContentsId) || m_ContentsId != licenseData.ContentsId)
                    {
                        Status = LicenseStatusEnum.Injustice;
                        throw new Exception($"injustice license. contentsId error.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "Activation", $"{ex.Message}");
            }
            return Status;
        }

        // 復号化して読込み
        protected string OnDecryptFromFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(m_EncryptKey))
                    throw new Exception("null or empty encryptKey.");

                if (string.IsNullOrEmpty(filePath))
                    throw new Exception("null or empty filePath.");

                if (!File.Exists(filePath))
                    throw new Exception($"not found filePath. path={filePath}");

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes(m_EncryptKey);
                    aesAlg.IV = Encoding.UTF8.GetBytes(m_EncryptKey);

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    using (CryptoStream cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "DecryptFromFile", $"{ex.Message}");
            }
            return string.Empty;
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(LicenseHandler))]
    public class LicenseHandlerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

            var handler = target as LicenseHandler;

            if (GUILayout.Button("CreateWindow"))
            {
                CreateEditorWindow.ShowWindow(handler);
            }

            EditorGUILayout.Space();

#endif

            DrawDefaultInspector();
        }

        public class CreateEditorWindow : EditorWindow
        {
            private static LicenseHandler m_handler = null;
            private static LicenseData m_createData = new LicenseData();

            private int _validityDaysIndex = 0;
            private int _validityDays = 0;

            public static void ShowWindow(LicenseHandler handler)
            {
                var window = GetWindow<CreateEditorWindow>();
                window.titleContent = new GUIContent("License");
                window.maxSize = new Vector2(400, 200);
                window.minSize = new Vector2(400, 200);

                m_handler = handler;

                m_createData.Uuid = string.Empty;
                m_createData.ContentsId = m_handler != null ? m_handler.m_ContentsId : string.Empty;
                m_createData.CreateDate = DateTime.UtcNow;
                m_createData.ValidityDays = 0;

                window.Show();
            }

            private void OnGUI()
            {
                GUILayout.Label("Create License");

                //-- 編集項目

                m_createData.Uuid = EditorGUILayout.TextField("Uuid", m_createData.Uuid);

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("ContentsId", new GUILayoutOption[] { GUILayout.Width(150) });

                if (m_handler != null)
                {
                    if (GUILayout.Button("Default", new GUILayoutOption[] { GUILayout.Height(20) }))
                    {
                        m_createData.ContentsId = m_handler.m_ContentsId;
                    }
                }

                m_createData.ContentsId = EditorGUILayout.TextField(m_createData.ContentsId);

                EditorGUILayout.EndHorizontal();

                var dayOptions = LicenseHandler.ValidityDaysList.Select(x => $"{x}day").ToArray();
                _validityDaysIndex = EditorGUILayout.Popup("ValidityDays", _validityDaysIndex, dayOptions);

                _validityDays = LicenseHandler.ValidityDaysList.Length <= _validityDaysIndex ? 0 : LicenseHandler.ValidityDaysList[_validityDaysIndex];

                m_createData.CreateDate = DateTime.UtcNow;
                m_createData.ValidityDays = _validityDays;

                EditorGUILayout.LabelField($"ExpiryDate(utc) [{_validityDays}day]: {m_createData.CreateDate:yyyy-MM-dd} -> {m_createData.ExpiryDate:yyyy-MM-dd}");

                //--

                GUILayout.Space(EditorGUIUtility.singleLineHeight);

                if (m_handler != null && m_createData.ValidityDays > 0)
                {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

                    if (GUILayout.Button("Create", new GUILayoutOption[] { GUILayout.ExpandWidth(true) }))
                    {
                        GUILayout.Space(EditorGUIUtility.singleLineHeight);

                        if (m_handler.Create(m_createData))
                        {
                            GUILayout.Label($"<color=yellow>Success</color>. path={m_handler.OutputFilePath}");
                        }
                        else
                        {
                            GUILayout.Label($"<color=red>Failed</color>.");
                        }
                    }

#endif
                }
            }
        }
    }

#endif
                }
