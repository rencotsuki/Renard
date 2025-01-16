using System;
using System.IO;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Renard.AssetBundleUniTask
{
    using Debuger;

    [Serializable]
    public class AssetBundleRequester
    {
        public string AssetName { get; private set; } = string.Empty;
        public string Path { get; private set; } = string.Empty;
        public bool IsEncrypt { get; private set; } = false;
        public string[] Dependencies { get; private set; } = null;
        public string StrDependencies { get; private set; } = string.Empty;
        public float EndTimeout { get; private set; } = 0f;
        public float Progress { get; private set; } = 0f;
        public bool IsDone { get; private set; } = false;
        public bool IsFileError { get; private set; } = false;
        public int RetryCount { get; private set; } = 0;
        public string ErrorMessage { get; private set; } = string.Empty;
        public AssetBundle AssetBundle { get; private set; } = null;

        protected bool isDebugLog = false;

        private uint _crc = 0;
        private AssetBundleCreateRequest _request = null;
        private CancellationTokenSource _cancellationTokenSource = null;

        protected void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!isDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(this.GetType(), logType, methodName, message);
        }

        public static AssetBundleRequester CreateRequest(string assetName, string path, uint crc, bool encrypt, bool isDebugLog, params string[] dependencies)
        {
            var load = new AssetBundleRequester();
            load.isDebugLog = isDebugLog;

            if (load.CreateTack(assetName, path, crc, encrypt, dependencies))
            {
                return load;
            }
            else
            {
                return null;
            }
        }

        ~AssetBundleRequester()
        {
            Dispose();
        }

        private bool CreateTack(string assetName, string path, uint crc, bool encrypt, string[] dependencies)
        {
            if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(path)) return false;

            AssetName = assetName;
            Path = path;
            IsEncrypt = encrypt;
            Dependencies = dependencies;
#if DEBUG
            foreach (var asset in Dependencies)
            {
                if (!string.IsNullOrEmpty(StrDependencies)) StrDependencies += ", ";
                StrDependencies += asset;
            }
#endif
            _crc = crc;

            Initialize();
            return true;
        }

        protected void Initialize()
        {
            RetryCount = 0;
            OnRequest();
        }

        public bool Retry()
        {
            if (RetryCount >= AssetBundleConfig.LoadRetryCount)
                return false;

            RetryCount++;
            OnRequest();
            return true;
        }

        private void OnRequest()
        {
            Progress = 0f;
            IsDone = false;
            IsFileError = false;
            ErrorMessage = string.Empty;

            EndTimeout = Time.time + AssetBundleConfig.LoadingTimeOut;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            OnLoadFromStreamAsync(_cancellationTokenSource.Token, AssetName, Path, _crc).Forget();
        }

        private async UniTask OnLoadFromStreamAsync(CancellationToken cancellationToken, string assetName, string path, uint crc)
        {
            try
            {
                Log(DebugerLogType.Info, "OnLoadFromStreamAsync", $"assetName={assetName}, crc={crc}, path={path}");

                using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Length <= 0)
                    {
                        IsFileError = true;
                        throw new Exception("File length zero.");
                    }

                    if (IsEncrypt)
                    {
                        var uniqueSalt = System.Text.Encoding.UTF8.GetBytes(assetName);
                        var uncryptor = new SeekableAesStream(fs, AssetBundleBuildConfig.Encrypt_KEY, uniqueSalt);
                        _request = AssetBundle.LoadFromStreamAsync(uncryptor, crc);
                        await _request.ToUniTask(Cysharp.Threading.Tasks.Progress.Create<float>(x => Progress = x), PlayerLoopTiming.FixedUpdate, cancellationToken);

                        AssetBundle = _request.assetBundle;
                    }
                    else
                    {
                        var loadAssetBundle = AssetBundle.LoadFromStreamAsync(fs, crc);
                        await loadAssetBundle.ToUniTask(Cysharp.Threading.Tasks.Progress.Create<float>(x => Progress = x), PlayerLoopTiming.FixedUpdate, cancellationToken);

                        AssetBundle = loadAssetBundle.assetBundle;
                    }

                    fs.Close();
                }

                IsDone = true;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnLoadFromStreamAsync", $"{ex.Message}");

                IsDone = true;
                ErrorMessage = ex.Message;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
