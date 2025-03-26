using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UniRx;

namespace Renard.Sample
{
    using QRCode;

    public class Launcher : MonoBehaviourCustom
    {
        [SerializeField] protected LicenseHandler licenseHandler = null;

        protected string uuid => DeviceUuidHandler.Uuid;

        protected LauncherConfigData configData = null;

        protected bool createLicenseMode
        {
            get
            {
#if UNITY_EDITOR
                if (CreateLicenseMode)
                    return true;
#endif
                return false;
            }
        }

        protected bool skipLicense
        {
            get
            {
#if UNITY_EDITOR
                if (SkipLicense)
                    return true;
#endif
                if (configData != null && configData.SkipLicense)
                    return true;
                return false;
            }
        }

        protected string licenseLocalPath
        {
            get
            {
                if (configData != null)
                    return configData.LicenseLocalPath;
                return LicenseHandler.DefaultLocalPath;
            }
        }

        protected bool retryStartup = false;

        protected bool pauseStartupAsync
        {
            get
            {
                if (SystemConsoleHandler.LicenseWindow.IsOpenWindow)
                    return true;

                if (SystemConsoleHandler.SystemWindow.IsOpenWindow)
                    return true;

                return false;
            }
        }

        protected SampleQRCamera sampleQRCamera = new SampleQRCamera();

        private CancellationTokenSource _startupToken = null;

        private void Awake()
        {
            var config = LauncherConfigAsset.Load();
            configData = config?.GetConfig();

            Application.targetFrameRate = configData != null ? configData.TargetFrameRate : LauncherConfigAsset.DefaultTargetFrameRate;
        }

        private void Start()
        {
            Startup();
        }

        private void OnDestroy()
        {
            OnDisposeStartup();
        }

        private void OnDisposeStartup()
        {
            _startupToken?.Dispose();
            _startupToken = null;
        }

        private void Startup()
        {
            OnDisposeStartup();
            _startupToken = new CancellationTokenSource();
            OnStartupAsync(_startupToken.Token).Forget();
        }

        private async UniTask OnStartupAsync(CancellationToken token)
        {
            try
            {
                retryStartup = false;

                if (createLicenseMode)
                {
                    // スプラッシュ表示が完了しているか確認する
                    await UniTask.WaitWhile(() => !SplashScreen.isFinished, cancellationToken: token);
                    token.ThrowIfCancellationRequested();

                    await SceneManager.LoadSceneAsync("CreateLicenseApp", LoadSceneMode.Single);
                    token.ThrowIfCancellationRequested();

                    return;
                }

                // ライセンス確認
                if (!await CheckLicenseAsync(token))
                {
                    if (retryStartup)
                        throw new Exception("retry startup.");

                    throw new Exception("license error.");
                }

                // スプラッシュ表示が完了しているか確認する
                await UniTask.WaitWhile(() => !SplashScreen.isFinished, cancellationToken: token);
                token.ThrowIfCancellationRequested();

                await SceneManager.LoadSceneAsync(configData != null ? configData.FirstSceneName : LauncherConfigAsset.DefaultFirstSceneName, LoadSceneMode.Single);
                token.ThrowIfCancellationRequested();

                // 追加シーンを生成
                CreateAdditiveScene();
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnStartupAsync", $"{ex.Message}");

                if (!token.IsCancellationRequested)
                {
                    if (retryStartup)
                    {
                        Startup();
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    }
                }
            }
        }

        private void CreateAdditiveScene()
        {
            // 投げ捨てで処理
            OnCreateAdditiveSceneAsync(new CancellationTokenSource().Token).Forget();
        }

        private async UniTask OnCreateAdditiveSceneAsync(CancellationToken token)
        {
            try
            {
                if (configData != null && configData.additiveScenes.Length > 0)
                {
                    foreach (var scene in configData.additiveScenes)
                    {
                        try
                        {
                            await SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                            token.ThrowIfCancellationRequested();
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"load additive Scenes error. scene={scene} :{ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnCreateAdditiveSceneAsync", $"{ex.Message}");
            }
        }

        private async UniTask<bool> CheckLicenseAsync(CancellationToken token)
        {
            try
            {
                if (string.IsNullOrEmpty(uuid))
                {
                    var closeWindow = false;

                    SystemConsoleHandler.SystemWindow
                        .SetMessage("起動エラー", "情報の取得に失敗しました", true)
                        .Show();

                    await UniTask.WaitWhile(() => !closeWindow, cancellationToken: token);
                    token.ThrowIfCancellationRequested();

                    throw new Exception("null or empty uuid.");
                }

                // ライセンスを確認
                var status = LicenseHandler.Activation(uuid, licenseLocalPath);
                if (status != LicenseStatusEnum.Success)
                {
                    if (!skipLicense)
                    {
                        var title = "ライセンス認証";
                        var message = string.Empty;
                        var errorMessage = string.Empty;

                        // ファイルがない場合
                        if (status == LicenseStatusEnum.NotFile)
                        {
                            // ライセンス発行の指示画面を表示
                            errorMessage = "not found license.";

                            CreateLicenseMessage();
                        }
                        else
                        {
                            if (status == LicenseStatusEnum.Expired)
                            {
                                // ライセンスの有効期限切れ
                                errorMessage = "expired license.";

                                message = $"ライセンスの有効期限が切れています\n\r有効期限: {LicenseHandler.ExpiryDate}";
                            }
                            else
                            {
                                // ライセンスの不正を表示
                                errorMessage = "injustice license.";

                                message = "ライセンスが正しくありません";
                            }

                            SystemConsoleHandler.SystemWindow
                                .SetMessage(title, message)
                                .OnActionDone(CreateLicenseMessage, "ライセンス依頼")
                                .OnActionCancel(null, "アプリを閉じる")
                                .Show();
                        }

                        await UniTask.WaitWhile(() => pauseStartupAsync, cancellationToken: token);
                        token.ThrowIfCancellationRequested();

                        throw new Exception(errorMessage);
                    }
                }

                await UniTask.NextFrame(token);
                return true;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "CheckLicenseAsync", $"{ex.Message}");
                return false;
            }
        }

        private void CreateLicenseMessage()
        {
            var title = "ライセンス依頼";
            var message = $"発行コード\n\r<size=20>{uuid}</size>";
            var qrCodeImage = QRCodeHelper.CreateQRCode(uuid, 128, 128, IsDebugLog);

            SystemConsoleHandler.LicenseWindow
                .SetMessage(title, message, qrCodeImage)
                .OnActionMain(() => GUIUtility.systemCopyBuffer = uuid, "コードコピー", false)
                .OnActionSub(ActivateLicenseMessage, "アクティベート", false)
                .Show();
        }

        private void ActivateLicenseMessage()
        {
            var title = "ライセンスアクティベート";
            var message = "ライセンスＱＲコードを\n\rカメラで読み取ります";

            var qrCodeImage = new Texture2D(256, 256);
            var qrCamera = sampleQRCamera?.Setup(256, 256);

            sampleQRCamera?.Play();

            SystemConsoleHandler.LicenseWindow
                .SetMessage(title, message, qrCamera, true)
                .OnActionMain(() =>
                {
                    if (LicenseHandler.ReadQRCodeToCreateFile(uuid, qrCamera, licenseLocalPath))
                    {
                        sampleQRCamera?.Stop();
                        retryStartup = true;
                        SystemConsoleHandler.LicenseWindow.Close();
                    }
                },
                "読み取り", false)
                .OnActionSub(() =>
                {
                    sampleQRCamera?.Stop();
                    CreateLicenseMessage();
                },
                "依頼画面に戻る", false)
                .Show();
        }

#if UNITY_EDITOR
        private static int _isCreateLicenseMode = -1;
        private const string _createLicenseMode = "CreateLicenseMode";

        protected static bool CreateLicenseMode
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                if (_isCreateLicenseMode == -1)
                    _isCreateLicenseMode = UnityEditor.EditorPrefs.GetBool(_createLicenseMode, true) ? 1 : 0;
                return _isCreateLicenseMode != 0;
#else
                return false;
#endif
            }
            set
            {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                int newValue = value ? 1 : 0;
                if (newValue != _isCreateLicenseMode)
                {
                    _isCreateLicenseMode = newValue;
                    UnityEditor.EditorPrefs.SetBool(_createLicenseMode, value);
                }
#endif
            }
        }

        private static int _isSkipLicense = -1;
        private const string _skiplicense = "SkipLicense";

        protected static bool SkipLicense
        {
            get
            {
                if (_isSkipLicense == -1)
                    _isSkipLicense = UnityEditor.EditorPrefs.GetBool(_skiplicense, true) ? 1 : 0;
                return _isSkipLicense != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != _isSkipLicense)
                {
                    _isSkipLicense = newValue;
                    UnityEditor.EditorPrefs.SetBool(_skiplicense, value);
                }
            }
        }

        [UnityEditor.MenuItem("Renard/License/CreateMode", false, 3)]
        public static void ToggleCreateLicenseMode()
        {
            CreateLicenseMode = !CreateLicenseMode;
        }

        [UnityEditor.MenuItem("Renard/License/CreateMode", true, 3)]
        public static bool ToggleCreateLicenseModeValidate()
        {
            UnityEditor.Menu.SetChecked("Renard/License/CreateMode", CreateLicenseMode);
            return true;
        }

        [UnityEditor.MenuItem("Renard/License/SkipLicense", false, 3)]
        public static void ToggleSkipLicense()
        {
            SkipLicense = !SkipLicense;
        }

        [UnityEditor.MenuItem("Renard/License/SkipLicense", true, 3)]
        public static bool ToggleSkipLicenseValidate()
        {
            UnityEditor.Menu.SetChecked("Renard/License/SkipLicense", SkipLicense);
            return true;
        }
#endif
    }
}
