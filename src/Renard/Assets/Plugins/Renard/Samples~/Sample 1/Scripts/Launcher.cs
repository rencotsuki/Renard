using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Renard.Sample
{
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

        private CancellationTokenSource _startupToken = null;

        private void Awake()
        {
            IsDebugLog = true;
            licenseHandler.IsDebugLog = IsDebugLog;

            var config = LauncherConfigAsset.Load();
            configData = config?.GetConfig();

            Application.targetFrameRate = configData != null ? configData.TargetFrameRate : LauncherConfigAsset.DefaultTargetFrameRate;
        }

        private void Start()
        {
            _startupToken = new CancellationTokenSource();
            OnStartupAsync(_startupToken.Token).Forget();
        }

        private async UniTask OnStartupAsync(CancellationToken token)
        {
            try
            {
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
                    throw new Exception("license error.");

                // ライセンス読込み確認のデバッグログ
                Debug.Log($"license: uuid={licenseHandler.Uuid}, contentsId={licenseHandler.ContentsId}, expiryDate={licenseHandler.ExpiryDate}");

                // スプラッシュ表示が完了しているか確認する
                await UniTask.WaitWhile(() => !SplashScreen.isFinished, cancellationToken: token);
                token.ThrowIfCancellationRequested();

                await SceneManager.LoadSceneAsync(configData != null ? configData.FirstSceneName : LauncherConfigAsset.DefaultFirstSceneName, LoadSceneMode.Single);
                token.ThrowIfCancellationRequested();

                if (configData != null && configData.additiveScenes.Length > 0)
                {
                    foreach (var scene in configData.additiveScenes)
                    {
                        await SceneManager.LoadSceneAsync(configData.FirstSceneName, LoadSceneMode.Additive);
                        token.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnStartupAsync", $"{ex.Message}");

                if (!token.IsCancellationRequested)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
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
                var status = licenseHandler.Activation(uuid, licenseLocalPath);
                if (status != LicenseStatusEnum.Success)
                {
                    if (!skipLicense)
                    {
                        var title = "ライセンス認証";
                        var message = string.Empty;
                        var errorMessage = string.Empty;
                        var closeWindow = false;

                        // ファイルがない場合
                        if (status == LicenseStatusEnum.NotFile)
                        {
                            // ライセンス発行の指示画面を表示
                            errorMessage = "not found license.";

                            message = $"ライセンスがありません\n\r発行してください\n\r\n\r発行コード\n\r<size=20>{uuid}</size>";

                            SystemConsoleHandler.SystemWindow
                                .SetMessage(title, message, true)
                                .OnActionDone(
                                () =>
                                {
                                    GUIUtility.systemCopyBuffer = uuid;
                                },
                                "ｺｰﾄﾞｺﾋﾟｰ",
                                false)
                                .OnActionClose(
                                () =>
                                {
                                    closeWindow = true;
                                })
                                .Show();
                        }
                        else if (status == LicenseStatusEnum.Expired)
                        {
                            if (status == LicenseStatusEnum.Expired)
                            {
                                // ライセンスの有効期限切れ
                                errorMessage = "expired license.";

                                message = $"ライセンスの有効期限が切れています\n\r有効期限: {licenseHandler.ExpiryDate}";
                            }
                            else
                            {
                                // ライセンスの不正を表示
                                errorMessage = "injustice license.";

                                message = "ライセンスが正しくありません";
                            }

                            SystemConsoleHandler.SystemWindow
                                .SetMessage(title, message, true)
                                .OnActionClose(() => { closeWindow = true; });
                        }

                        await UniTask.WaitWhile(() => !closeWindow, cancellationToken: token);
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
