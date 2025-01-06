using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Renard
{
    public class Launcher : MonoBehaviourCustom
    {
        protected string FirstScene => "Sample";

        private CancellationTokenSource _startupToken = null;

        private void Start()
        {
            _startupToken = new CancellationTokenSource();
            OnStartupAsync(_startupToken.Token).Forget();
        }

        private async UniTask OnStartupAsync(CancellationToken token)
        {
            // ライセンス確認
            if (true)
            {
                await OnSuccessStartupAsync(token);
            }
            else
            {
                await OnFailedStartupAsync(token, "ライセンス認証エラー");
            }
        }

        private async UniTask OnSuccessStartupAsync(CancellationToken token)
        {
            await SceneManager.LoadSceneAsync(FirstScene, LoadSceneMode.Single);
            token.ThrowIfCancellationRequested();
        }

        private async UniTask OnFailedStartupAsync(CancellationToken token, string messege)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3f), cancellationToken: token);
            token.ThrowIfCancellationRequested();

            Application.Quit();
        }
    }
}
