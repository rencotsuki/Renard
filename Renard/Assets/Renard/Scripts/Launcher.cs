using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Renard
{
    public class Launcher : MonoBehaviourCustom
    {
        [SerializeField] protected LicenseHandler licenseHandler = null;

        protected LauncherConfigData configData = null;

        protected bool skipLicense
        {
            get
            {
#if UNITY_EDITOR
                if (!LicenseSimulation)
                    return true;
#endif
                return false;
            }
        }

        private CancellationTokenSource _startupToken = null;

        private void Awake()
        {
            isDebugLog = true;

            var config = LauncherConfig.Load();
            configData = config?.GetConfig();

            Application.targetFrameRate = configData != null ? configData.TargetFrameRate : LauncherConfig.DefaultTargetFrameRate;
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
                // ライセンス確認
                if (!await CheckLicenseAsync(token))
                    throw new Exception("license error.");

                // スプラッシュ表示が完了しているか確認する
                await UniTask.WaitWhile(() => !SplashScreen.isFinished, cancellationToken: token);
                token.ThrowIfCancellationRequested();

                await SceneManager.LoadSceneAsync(configData != null ? configData.FirstSceneName : LauncherConfig.DefaultFirstSceneName, LoadSceneMode.Single);
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
                    Application.Quit();
            }
        }

        private async UniTask<bool> CheckLicenseAsync(CancellationToken token)
        {
            try
            {
                if (string.IsNullOrEmpty(DeviceUuidHandler.Uuid))
                {
                    // エラーコード発行

                    throw new Exception("null or empty uuid.");
                }

                // ライセンスを確認
                var status = licenseHandler.Activation(DeviceUuidHandler.Uuid);
                if (status != LicenseStatusEnum.Success)
                {
                    if (!skipLicense)
                    {
                        // ファイルがない場合
                        if (status != LicenseStatusEnum.NotFile)
                        {
                            // ライセンス発行の指示画面を表示
                            throw new Exception("not found license.");
                        }

                        // ライセンスの有効期限切れ
                        if (status != LicenseStatusEnum.Expired)
                        {
                            // ライセンス有効期限切れを表示
                            throw new Exception("expired license.");
                        }

                        // ライセンスの不正を表示
                        throw new Exception("injustice license.");
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
        private static int _isLicenseSimulation = -1;
        private const string _licenseSimulation = "LicenseSimulation";

        protected static bool LicenseSimulation
        {
            get
            {
                if (_isLicenseSimulation == -1)
                    _isLicenseSimulation = UnityEditor.EditorPrefs.GetBool(_licenseSimulation, true) ? 1 : 0;
                return _isLicenseSimulation != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != _isLicenseSimulation)
                {
                    _isLicenseSimulation = newValue;
                    UnityEditor.EditorPrefs.SetBool(_licenseSimulation, value);
                }
            }
        }

        [UnityEditor.MenuItem("Renard/LicenseSimulation", false, 1)]
        public static void ToggleLicenseSimulation()
        {
            LicenseSimulation = !LicenseSimulation;
        }

        [UnityEditor.MenuItem("Renard/LicenseSimulation", true, 1)]
        public static bool ToggleLicenseSimulationValidate()
        {
            UnityEditor.Menu.SetChecked("Renard/LicenseSimulation", LicenseSimulation);
            return true;
        }
#endif
    }
}
