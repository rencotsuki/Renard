using System;
using System.Linq;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renard
{
    using License;
    using QRCode;
    using Debuger;

    public sealed class LicenseHandler
    {
        public const string DefaultLocalPath = "License";

        public static string OutputPath
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

        private static string activationFilePath
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

        public static LicenseStatusEnum Status { get; private set; } = LicenseStatusEnum.None;

        public static int[] ValidityDaysList => new int[] { 7, 14, 21, 30, 60, 120, 180, 210, 240, 270, 300, 330, 365 };

        private static LicenseData licenseData = default;

        //-- LicenseDataの見せても良い情報だけPublic定義
        public static string LicensePassKey => licenseData.LicensePassKey;
        public static DateTime CreateDateTime => licenseData.CreateDate;
        public static string CreateDate => $"{CreateDateTime:yyyy-MM-dd}";
        public static int ValidityDays => licenseData.ValidityDays;
        public static DateTime ExpiryDateTime => licenseData.ExpiryDate;
        public static string ExpiryDate => $"{ExpiryDateTime:yyyy-MM-dd}";
        //--

        private static LicenseConfigAsset _licenseConfig = null;
        private static LicenseConfigAsset licenseConfig
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
                    Log(DebugerLogType.Info, "LicenseConfig", $"{ex.Message}");
                    _licenseConfig = null;
                }
                return _licenseConfig;
            }
        }
        public static string LicenseFileExtension
        {
            get
            {
                if (licenseConfig != null)
                    return licenseConfig.LicenseFileExtension;
                return string.Empty;
            }
        }
        public static string ConfigContentsId
        {
            get
            {
                if (licenseConfig != null)
                    return licenseConfig.ContentsId;
                return string.Empty;
            }
        }
        private static string m_EncryptKey =>licenseConfig != null ? licenseConfig.EncryptKey : string.Empty;
        private static string m_EncryptIV => licenseConfig != null ? licenseConfig.EncryptIV : string.Empty;

        private static string fileName => $"License-{Application.productName}";
        private static string fileFullName => $"{(string.IsNullOrEmpty(LicenseFileExtension) ? fileName : $"{fileName}.{LicenseFileExtension}")}";
        private static string imageFileExtension => "png";
        private static string imageFullName => $"LicenseQRCode-{Application.productName}.{imageFileExtension}";

        private static bool _isDebugLog = false;

        private static void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!_isDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(typeof(LicenseManager), logType, methodName, message);
        }

        /// <summary>時間取得(UtcNow)</summary>
        public static DateTime GetNow() => DateTime.UtcNow;

        /// <summary>ライセンスファイル生成</summary>
        public static bool Create(LicenseData data, bool isDebugLog = false)
        {
            return Create(data, out Texture2D qrCore, Vector2Int.zero, isDebugLog);
        }

        // 暗号化して保存
        private static bool OnEncryptAndSaveToFile(string licenseCode, string outputPath, out Texture2D qrCore, Vector2Int qrCoreSize)
        {
            qrCore = null;

            try
            {
                var encryptCode = AESGenerator.Encrypt(licenseCode, m_EncryptKey, m_EncryptIV, _isDebugLog);
                if (string.IsNullOrEmpty(encryptCode))
                    throw new Exception("encrypt error.");

                // QRコード生成
                if (qrCoreSize.x > 0 && qrCoreSize.y > 0)
                {
                    qrCore = QRCodeHelper.CreateQRCode(encryptCode, qrCoreSize.x, qrCoreSize.y, _isDebugLog);

                    if (!OnWriteImage(qrCore, outputPath, imageFullName))
                    {
                        Log(DebugerLogType.Warning, "OnEncryptAndSaveToFile", "create QRcode image failed.");
                    }
                }

                return OnWriteFile(encryptCode, outputPath, fileFullName);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "OnEncryptAndSaveToFile", $"{ex.Message}");
            }
            return false;
        }

        // ファイルに保存
        private static bool OnWriteFile(string dataString, string outputPath, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(dataString))
                    throw new Exception("null or empty write data.");

                if (string.IsNullOrEmpty(outputPath))
                    throw new Exception("null or empty outputPath.");

                if (string.IsNullOrEmpty(fileName))
                    throw new Exception("null or empty fileName.");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                using (FileStream fs = new FileStream($"{outputPath}/{fileName}", FileMode.Create))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(dataString);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "OnWriteFile", $"{ex.Message}");
            }
            return false;
        }

        // 画像ファイルに保存
        private static bool OnWriteImage(Texture2D imageData, string outputPath, string fileName)
        {
            try
            {
                if (imageData == null)
                    throw new Exception("null write image.");

                if (string.IsNullOrEmpty(outputPath))
                    throw new Exception("null or empty outputPath.");

                if (string.IsNullOrEmpty(fileName))
                    throw new Exception("null or empty fileName.");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                var imageBinary = imageData.EncodeToPNG();
                File.WriteAllBytes($"{outputPath}/{fileName}", imageBinary);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "OnWriteImage", $"{ex.Message}");
            }
            return false;
        }

        /// <summary>ライセンス確認</summary>
        public static LicenseStatusEnum Activation(string uuid, string localPath = DefaultLocalPath, bool isDebugLog = false)
        {
            Status = LicenseStatusEnum.None;
            licenseData = default;
            _isDebugLog = isDebugLog;

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
                var licenseCode = OnDecryptFromFile($"{filePath}/{fileFullName}");
                if (licenseCode == null || licenseCode.Length <= 0)
                {
                    Status = LicenseStatusEnum.NotFile;
                    throw new Exception($"not found file. path={filePath}/{fileFullName}");
                }

                Status = OnValidateLicense(uuid, licenseCode, out licenseData);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "Activation", $"{ex.Message}");
            }
            return Status;
        }

        // ライセンスチェック
        private static LicenseStatusEnum OnValidateLicense(string uuid, string licenseCode, out LicenseData licenseData)
        {
            licenseData = default;

            try
            {
                if (licenseConfig == null)
                    throw new Exception("not found licenseConfig.");

                Status = LicenseManager.ValidateLicense(licenseConfig, licenseCode, out licenseData, _isDebugLog);

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
                Log(DebugerLogType.Warning, "OnValidateLicense", $"{ex.Message}");
            }
            return Status;
        }

        // 復号化
        private static string OnDecryptCode(string dataCode)
        {
            try
            {
                if (string.IsNullOrEmpty(dataCode))
                    throw new Exception("null or empty dataCode.");

                var decryptCode = AESGenerator.Decrypt(dataCode, m_EncryptKey, m_EncryptIV, _isDebugLog);
                if (string.IsNullOrEmpty(decryptCode))
                    throw new Exception("decrypt error.");

                return decryptCode;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "OnDecryptCode", $"{ex.Message}");
            }
            return string.Empty;
        }

        // ファイルを読込んで復号化
        private static string OnDecryptFromFile(string fileFullPath)
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

                return OnDecryptCode(encryptCode);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "DecryptFromFile", $"{ex.Message}");
            }
            return string.Empty;
        }

        /// <summary>ライセンスファイル生成（QRCodeも生成）</summary>
        public static bool Create(LicenseData data, out Texture2D qrCore, Vector2Int qrCoreSize, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;
            qrCore = null;

            try
            {
                if (licenseConfig == null)
                {
                    Debug.Log("<color=yellow>【重要】</color> LicenseConfig.assetが定義されていません。");
                    throw new Exception("not found licenseConfig.");
                }

                // ライセンスコードを生成
                var licenseCode = LicenseManager.GenerateLicense(licenseConfig, data, _isDebugLog);
                return OnEncryptAndSaveToFile(licenseCode, OutputPath, out qrCore, qrCoreSize);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "Create", $"{ex.Message}");
            }
            return false;
        }

        /// <summary>ライセンスＱＲコード読込みとファイル生成</summary>
        public static bool ReadQRCodeToCreateFile(string uuid, Texture2D readQRCore, string localPath = DefaultLocalPath, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            try
            {
                if (readQRCore == null)
                    throw new Exception("not qrcode texture.");

                // QRCodeの読み取り
                var encryptCode = QRCodeHelper.Read(readQRCore, _isDebugLog);

                return OnReadQRCodeToCreateFile(uuid, encryptCode, localPath);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "ReadQRCodeToCreateFile", $"[Texture2D] {ex.Message}");
            }
            return false;
        }

        /// <summary>ライセンスＱＲコード読込みとファイル生成</summary>
        public static bool ReadQRCodeToCreateFile(string uuid, WebCamTexture readQRCore, string localPath = DefaultLocalPath, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            try
            {
                if (readQRCore == null)
                    throw new Exception("not qrcode texture.");

                // QRCodeの読み取り
                var encryptCode = QRCodeHelper.Read(readQRCore, _isDebugLog);

                return OnReadQRCodeToCreateFile(uuid, encryptCode, localPath);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "ReadQRCodeToCreateFile", $"[WebCamTexture] {ex.Message}");
            }
            return false;
        }

        private static bool OnReadQRCodeToCreateFile(string uuid, string encryptCode, string localPath = DefaultLocalPath)
        {
            try
            {
                var licenseCode = OnDecryptCode(encryptCode);

                // ライセンス確認をしてから保存する
                var status = OnValidateLicense(uuid, licenseCode, out LicenseData licenseData);
                if (status != LicenseStatusEnum.Success)
                    throw new Exception("failed validate license.");

                var localPathTrim = string.IsNullOrEmpty(localPath) ? string.Empty : localPath.TrimStart().TrimEnd();
                var filePath = string.IsNullOrEmpty(localPathTrim) ? activationFilePath : $"{activationFilePath}/{localPathTrim}";

                return OnWriteFile(encryptCode, filePath, fileFullName);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "ReadQRCodeToCreateFile", $"{ex.Message}");
            }
            return false;
        }

#if UNITY_EDITOR

        [MenuItem("Renard/License/Open CreateLicenseEditor", false)]
        private static void OpenCreateLicenseEditor()
        {
            CreateEditorWindow.ShowWindow();
        }

#endif
    }

#if UNITY_EDITOR

    // ライセンス発行ウィンドウ(Editor)
    public class CreateEditorWindow : EditorWindow
    {
        private static LicenseData m_createData = new LicenseData();

        private static readonly Vector2Int qrCodeSize = new Vector2Int(128, 128);

        private int _validityDaysIndex = 0;
        private int _validityDays = 0;
        private bool _createSuccess = false;
        private Texture2D _qrCodeTexture = null;

        public static void ShowWindow()
        {
            var window = GetWindow<CreateEditorWindow>();
            window.titleContent = new GUIContent("License");
            window.maxSize = new Vector2(400, 400);
            window.minSize = new Vector2(400, 400);

            m_createData.Uuid = string.Empty;
            m_createData.ContentsId = LicenseHandler.ConfigContentsId;
            m_createData.CreateDate = DateTime.UtcNow;
            m_createData.ValidityDays = 0;

            window.Show();
        }

        private void OnGUI()
        {
            try
            {
                DrawUI();
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(CreateEditorWindow).Name}::OnGUI - {ex.Message}");
            }
        }

        public void SetUuid(string value)
        {
            m_createData.Uuid = value;
        }

        private void DrawUI()
        {
            GUILayout.Label("Create License");

            //-- 編集項目

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Uuid", new GUILayoutOption[] { GUILayout.ExpandWidth(true) });

            if (GUILayout.Button("QRCode", new GUILayoutOption[] { GUILayout.Height(20), GUILayout.Width(100) }))
            {
                ReadQRCodeEditorWindow.ShowWindow(this);
            }

            EditorGUILayout.EndHorizontal();

            m_createData.Uuid = EditorGUILayout.TextField(m_createData.Uuid);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("ContentsId", new GUILayoutOption[] { GUILayout.Width(150) });

            if (GUILayout.Button("Default", new GUILayoutOption[] { GUILayout.Height(20) }))
            {
                m_createData.ContentsId = LicenseHandler.ConfigContentsId;
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

            if (m_createData.ValidityDays > 0)
            {
                if (GUILayout.Button("Create", new GUILayoutOption[] { GUILayout.ExpandWidth(true) }))
                {
                    _createSuccess = LicenseHandler.Create(m_createData, out _qrCodeTexture, qrCodeSize, true);
                }
            }

            EditorGUILayout.LabelField("【Status】");

            if (_createSuccess)
            {
                EditorGUILayout.LabelField("Success!");

                EditorGUILayout.LabelField("[Path]");
                var style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;
                EditorGUILayout.LabelField($"{LicenseHandler.OutputPath}", style, GUILayout.ExpandWidth(true));

                if (_qrCodeTexture != null)
                {
                    EditorGUILayout.LabelField("[QRCode]");
                    EditorGUILayout.LabelField(new GUIContent(_qrCodeTexture), GUILayout.Height(qrCodeSize.x), GUILayout.Width(qrCodeSize.y));
                }
            }
        }
    }

    // QRコード読取りウィンドウ(Editor)
    public class ReadQRCodeEditorWindow : EditorWindow
    {
        private static ReadQRCodeEditorWindow window = null;
        private static WebCamTexture webcamTexture = null;
        private static readonly Vector2Int qrCodeSize = new Vector2Int(256, 256);
        private static readonly int fps = 30;
        private static CreateEditorWindow createWindow = null;

        private bool isOpenWindow = false;
        private string uuid = string.Empty;

        public static void ShowWindow(CreateEditorWindow target)
        {
            if (target == null)
                return;

            if (window != null && window.isOpenWindow)
                return;

            createWindow = target;

            window = GetWindow<ReadQRCodeEditorWindow>();
            window.titleContent = new GUIContent("ReadQRCode");
            window.maxSize = new Vector2(400, 300);
            window.minSize = new Vector2(400, 300);

            if (StartCamera())
            {
                window.isOpenWindow = true;
                window.ShowPopup();
            }
        }

        public static void CloseWindow()
        {
            StopCamera();

            if (window != null)
            {
                window.isOpenWindow = false;
                window.Close();
            }

            window = null;
        }

        private void OnLostFocus()
        {
            // フォーカスを失ったら閉じる
            CloseWindow();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            try
            {
                if (createWindow != null)
                {
                    DrawUI();
                }
                else
                {
                    CloseWindow();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(CreateEditorWindow).Name}::OnGUI - {ex.Message}");
            }
        }

        private static bool StartCamera()
        {
            try
            {
                StopCamera();

                webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name, qrCodeSize.x, qrCodeSize.y, fps);
                webcamTexture.Play();
                return true;
            }
            catch
            {
                // 何もしない
            }
            return false;
        }

        private static void StopCamera()
        {
            webcamTexture?.Stop();
            webcamTexture = null;
        }

        private void DrawUI()
        {
            GUILayout.Label("QRCode Reader");

            if (webcamTexture != null)
            {
                // 中央寄せ
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(new GUIContent(webcamTexture), GUILayout.Height(qrCodeSize.x), GUILayout.Width(qrCodeSize.y));
                    GUILayout.FlexibleSpace();
                }
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Read", new GUILayoutOption[] { GUILayout.Height(20) }))
            {
                if (webcamTexture != null)
                {
                    uuid = QRCodeHelper.Read(webcamTexture);
                    if (!string.IsNullOrEmpty(uuid))
                    {
                        createWindow?.SetUuid(uuid);
                        CloseWindow();
                    }
                }
            }

            if (GUILayout.Button("Close", new GUILayoutOption[] { GUILayout.Height(20) }))
            {
                CloseWindow();
            }

            EditorGUILayout.EndHorizontal();
        }
    }

#endif
}
