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
        protected LauncherConfigData configData = null;

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

                // ライセンスキーの存在確認
                if (false)
                {
                    // ライセンス発行の指示画面を表示する

                    throw new Exception("not found license key.");
                }

                // ライセンスキーと照合する
                if (false)
                {
                    // エラーコード発行

                    throw new Exception("discrepancy license.");
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
    }
}
