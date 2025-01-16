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
        private static bool IsDebugLog => AssetBundleManager.IsDebugLogMaster;
        private static int LoadingTimeOutMilliseconds => AssetBundleManager.LoadingTimeOutMilliseconds;
        private static int LoadRetryCount => AssetBundleManager.LoadRetryCount;

        public string AssetName { get; private set; } = string.Empty;
        public string Path { get; private set; } = string.Empty;
        public string[] Dependencies { get; private set; } = null;
        public string StrDependencies { get; private set; } = string.Empty;
        public float EndTimeout { get; private set; } = 0f;
        public float Progress { get; private set; } = 0f;
        public bool IsDone { get; private set; } = false;
        public bool IsFileError { get; private set; } = false;
        public int RetryCount { get; private set; } = 0;
        public string ErrorMessage { get; private set; } = string.Empty;
        public AssetBundle AssetBundle { get; private set; } = null;

        private bool _isEncrypt = false;
        private string _encryptKey = string.Empty;
        private uint _crc = 0;
        private AssetBundleCreateRequest _request = null;
        private CancellationTokenSource _cancellationTokenSource = null;

        protected void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!IsDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(this.GetType(), logType, methodName, message);
        }

        public static AssetBundleRequester CreateRequest(string assetName, string path, uint crc, bool encrypt, string encryptKey, params string[] dependencies)
        {
            var load = new AssetBundleRequester();
            if (load.CreateTack(assetName, path, crc, encrypt, encryptKey, dependencies))
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

        private bool CreateTack(string assetName, string path, uint crc, bool encrypt, string encryptKey, string[] dependencies)
        {
            if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(path)) return false;

            AssetName = assetName;
            Path = path;
            Dependencies = dependencies;
#if DEBUG
            foreach (var asset in Dependencies)
            {
                if (!string.IsNullOrEmpty(StrDependencies)) StrDependencies += ", ";
                StrDependencies += asset;
            }
#endif
            _crc = crc;
            _isEncrypt = encrypt;
            _encryptKey = encryptKey;

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
            if (RetryCount >= LoadRetryCount)
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
            EndTimeout = Time.time + ((float)LoadingTimeOutMilliseconds * 0.001f);

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            OnLoadFromStreamAsync(_cancellationTokenSource.Token, AssetName, Path).Forget();
        }

        private async UniTask OnLoadFromStreamAsync(CancellationToken cancellationToken, string assetName, string path)
        {
            try
            {
                Log(DebugerLogType.Info, "OnLoadFromStreamAsync", $"assetName={assetName}, crc={_crc}, encrypt={_isEncrypt}, path={path}");

                using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Length <= 0)
                    {
                        IsFileError = true;
                        throw new Exception("File length zero.");
                    }

                    if (_isEncrypt)
                    {
                        var uniqueSalt = System.Text.Encoding.UTF8.GetBytes(assetName);
                        var uncryptor = new SeekableAesStream(fs, _encryptKey, uniqueSalt);
                        _request = AssetBundle.LoadFromStreamAsync(uncryptor, _crc);
                        await _request.ToUniTask(Cysharp.Threading.Tasks.Progress.Create<float>(x => Progress = x), PlayerLoopTiming.FixedUpdate, cancellationToken);

                        AssetBundle = _request.assetBundle;
                    }
                    else
                    {
                        var loadAssetBundle = AssetBundle.LoadFromStreamAsync(fs, _crc);
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
