using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

namespace Renard
{
    using License;

    public class LicenseHandler : MonoBehaviourCustom
    {
        protected string fileName => $"License-{Application.productName}";
        protected string fileExtension => ""; //拡張子を付けられるようにしておく
        public string FileFullName => $"{(string.IsNullOrEmpty(fileExtension) ? fileName : $"{fileName}.{fileExtension}")}";

        public const string DefaultLocalPath = "License";

        public string OutputPath
        {
            get
            {
#if UNITY_EDITOR
                return $"{Application.dataPath}/../../CreateLicenses";
#else
                return $"{Application.dataPath}/../CreateLicenses";
#endif
            }
        }

        protected string activationFilePath
        {
            get
            {
#if UNITY_EDITOR
                return $"{Application.dataPath}/../..";
#else
                return Application.persistentDataPath;
#endif
            }
        }

        public LicenseStatusEnum Status { get; protected set; } = LicenseStatusEnum.None;

        public static int[] ValidityDaysList => new int[] { 7, 14, 21, 30, 60, 120, 180, 210, 240, 270, 300, 330, 365 };

        protected LicenseData licenseData = default;
        public string Uuid => licenseData.Uuid;
        public string ContentsId => licenseData.ContentsId;
        public string LicensePassKey => licenseData.LicensePassKey;
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
        public string ConfigContentsId
        {
            get
            {
                if (licenseConfig != null)
                    return licenseConfig.ContentsId;
                return string.Empty;
            }
        }
        protected string m_EncryptKey =>licenseConfig != null ? licenseConfig.EncryptKey : string.Empty;
        protected string m_EncryptIV => licenseConfig != null ? licenseConfig.EncryptIV : string.Empty;

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
                var licenseCode = LicenseManager.GenerateLicense(licenseConfig, data, IsDebugLog);
                return OnEncryptAndSaveToFile(licenseCode, OutputPath, FileFullName);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "Encrypt", $"{ex.Message}");
            }
            return false;
        }

        // 暗号化して保存
        protected bool OnEncryptAndSaveToFile(string licenseCode, string outputPath, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(outputPath))
                    throw new Exception("null or empty outputPath.");

                if (string.IsNullOrEmpty(fileName))
                    throw new Exception("null or empty fileName.");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                var encryptCode = LicenseManager.EncryptCode(licenseCode, m_EncryptKey, m_EncryptIV, IsDebugLog);
                if (string.IsNullOrEmpty(encryptCode))
                    throw new Exception("encrypt error.");

                using (FileStream fs = new FileStream($"{outputPath}/{fileName}", FileMode.Create))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(encryptCode);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "OnEncryptAndSaveToFile", $"{ex.Message}");
            }
            return false;
        }

        /// <summary>ライセンス確認</summary>
        public LicenseStatusEnum Activation(string uuid, string localPath = DefaultLocalPath)
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

                var localPathTrim = string.IsNullOrEmpty(localPath) ? string.Empty : localPath.TrimStart().TrimEnd();
                var filePath = string.IsNullOrEmpty(localPathTrim) ? activationFilePath : $"{activationFilePath}/{localPathTrim}";

                // ライセンスコードの格納先を確認
                if (!Directory.Exists(filePath))
                {
                    // ディレクトリは変わらないので作成しておく
                    Directory.CreateDirectory(filePath);

                    Status = LicenseStatusEnum.NotFile;
                    throw new Exception($"not found directory. path={filePath}");
                }

                // ライセンスコードを読込み
                var licenseCode = OnDecryptFromFile($"{filePath}/{FileFullName}");
                if (licenseCode == null || licenseCode.Length <= 0)
                {
                    Status = LicenseStatusEnum.NotFile;
                    throw new Exception($"not found file. path={filePath}/{FileFullName}");
                }

                Status = LicenseManager.ValidateLicense(licenseConfig, licenseCode, out licenseData, IsDebugLog);

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
                    if (string.IsNullOrEmpty(ConfigContentsId) || ConfigContentsId != licenseData.ContentsId)
                    {
                        Status = LicenseStatusEnum.Injustice;
                        throw new Exception($"injustice license. contentsId error.");
                    }

                    /*
                     * licensePassKeyのチェックはアプリ側で判断する
                     */
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "Activation", $"{ex.Message}");
            }
            return Status;
        }

        // 復号化して読込み
        protected string OnDecryptFromFile(string fileFullPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fileFullPath))
                    throw new Exception("null or empty filePath.");

                if (!File.Exists(fileFullPath))
                    throw new Exception($"not found filePath. path={fileFullPath}");

                var encryptCode = string.Empty;
                using (FileStream fs = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
                using (StreamReader sr = new StreamReader(fs))
                {
                    encryptCode = sr.ReadToEnd();
                }

                var decryptCode = LicenseManager.DecryptCode(encryptCode, m_EncryptKey, m_EncryptIV, IsDebugLog);
                if (string.IsNullOrEmpty(decryptCode))
                    throw new Exception("decrypt error.");

                return decryptCode;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "DecryptFromFile", $"{ex.Message}");
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
            var handler = target as LicenseHandler;

            if (GUILayout.Button("CreateWindow"))
            {
                CreateEditorWindow.ShowWindow(handler);
            }

            EditorGUILayout.Space();

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
                m_createData.ContentsId = m_handler != null ? m_handler.ConfigContentsId : string.Empty;
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
                        m_createData.ContentsId = m_handler.ConfigContentsId;
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
                    if (GUILayout.Button("Create", new GUILayoutOption[] { GUILayout.ExpandWidth(true) }))
                    {
                        GUILayout.Space(EditorGUIUtility.singleLineHeight);

                        if (m_handler.Create(m_createData))
                        {
                            GUILayout.Label($"<color=yellow>Success</color>. path={m_handler.OutputPath}/{m_handler.OutputPath}");
                        }
                        else
                        {
                            GUILayout.Label($"<color=red>Failed</color>.");
                        }
                    }
                }
            }
        }
    }

#endif
}
