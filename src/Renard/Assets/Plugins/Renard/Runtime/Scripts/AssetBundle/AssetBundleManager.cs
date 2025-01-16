using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniRx;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renard
{
    using AssetBundleUniTask;
    using AssetBundle = UnityEngine.AssetBundle;

    public class AssetBundleManager : SingletonMonoBehaviourCustom<AssetBundleManager>
    {
        public const int LoadingTimeOutMilliseconds = 30 * 1000;
        public const int LoadRetryCount = 5;

        public const string ManifestFileExtension = "manifest";
        public const string HashFileExtension = "json";
        public const string LocalPath = "AssetBundles";
        public const string HashFileName = "AssetBundleHash";
        public const string DefaultOutputPath = "AssetBundles";

        public static bool IsDebugLogMaster => singleton != null ? singleton.IsDebugLog : false;

        public bool IsSetup { get; private set; } = false;
        public bool IsInit { get; private set; } = false;
        public bool IsDiffData { get; private set; } = false;

        public bool IsServer { get; private set; } = false;
        public bool IsSimulateMode
        {
            get
            {
//#if UNITY_EDITOR
//                if (AssetBundleEditor.IsSimulateMode)
//                    return true;
//#endif
                return false;
            }
        }

        private const int readTimeoutMillisecond = 5 * 1000;

        private const int deleteWaitTimeoutMillisecond = 500;

        protected RuntimePlatform activePlatform
        {
            get
            {

#if UNITY_EDITOR
                return ToRuntimePlatform(EditorUserBuildSettings.activeBuildTarget);
#else
                return Application.platform;
#endif
            }
        }

        public string ManifestName
        {
            get
            {
#if UNITY_EDITOR
                return GetPlatformManifestName(EditorUserBuildSettings.activeBuildTarget);
#else
                return GetPlatformManifestName(activePlatform);
#endif
            }
        }

        private AssetBundleConfigAsset _assetBundleConfig = null;
        protected AssetBundleConfigAsset assetBundleConfig
        {
            get
            {
                try
                {
                    if (_assetBundleConfig == null)
                        _assetBundleConfig = AssetBundleConfigAsset.Load();
                }
                catch (Exception ex)
                {
                    Log(DebugerLogType.Info, "assetBundleConfig", $"{ex.Message}");
                    _assetBundleConfig = null;
                }
                return _assetBundleConfig;
            }
        }
        protected bool isEncrypt => assetBundleConfig != null ? assetBundleConfig.IsEncrypt : false;
        protected string encryptIV => assetBundleConfig != null ? assetBundleConfig.EncryptIV : string.Empty;
        protected string encryptKey => assetBundleConfig != null ? assetBundleConfig.EncryptKey : string.Empty;

        public string DirectoryPath { get; private set; } = string.Empty;
        public string AssetDirectoryPath
        {
            get
            {
#if UNITY_EDITOR
                return $"{DirectoryPath}/{LocalPath}/{GetPlatformDirectoryName(activePlatform)}";
#else
                return $"{DirectoryPath}/{LocalPath}/{GetPlatformDirectoryName(activePlatform)}";
#endif
            }
        }

        protected enum DependencyLoad
        {
            NoTargets,
            LoadWait,
            Success,
            Failed,
        };

        protected string[] activeVariants = default;
        public AssetBundleManifest Manifest = null;

        protected Dictionary<string, LoadedAssetBundle> loadedAssetBundles = new Dictionary<string, LoadedAssetBundle>();
        protected Dictionary<string, AssetBundleRequester> loadingStreamingAssets = new Dictionary<string, AssetBundleRequester>();
        protected Dictionary<string, string> loadingStreamingAssetsErrors = new Dictionary<string, string>();
        protected Dictionary<string, string> assetBundleFileErrors = new Dictionary<string, string>();
        protected List<AssetBundleLoadOperation> inProgressOperations = new List<AssetBundleLoadOperation>();

        private Dictionary<string, string[]> _dependencies = new Dictionary<string, string[]>();

        public AssetBundleHashList HashList { get; private set; } = null;

        public (byte[], DateTime) OriginHashList_Active
        {
            get
            {
                if (activePlatform == RuntimePlatform.WindowsPlayer) return originHashList_Win;
                if (activePlatform == RuntimePlatform.OSXPlayer)     return originHashList_OSX;
                if (activePlatform == RuntimePlatform.IPhonePlayer)  return originHashList_iOS;
                if (activePlatform == RuntimePlatform.Android)       return originHashList_And;

                return (null, DateTime.UtcNow);
            }
        }

        protected (byte[], DateTime) originHashList_Win { get; private set; } = default;
        protected (byte[], DateTime) originHashList_OSX { get; private set; } = default;
        protected (byte[], DateTime) originHashList_iOS { get; private set; } = default;
        protected (byte[], DateTime) originHashList_And { get; private set; } = default;

        public string[] ActiveVariants
        {
            get { return activeVariants; }
            set { activeVariants = value; }
        }

        #region Progress

        private float _progress = 0f;
        private string _progressStatus = string.Empty;

        protected void SetProgress(float progress) => _progress = progress < 0 ? 0f : progress > 1f ? 1f : progress;
        protected void SetProgressStatus(string log, bool add = false) => _progressStatus = add ? $"{_progressStatus}{log}" : log;
        protected void ResetProgress()
        {
            _progress = 0f;
            _progressStatus = string.Empty;
        }

        public static string ProgressStatus => singleton != null ? singleton._progressStatus : string.Empty;
        public static float Progress => singleton != null ? singleton._progress : 0f;

        #endregion

        public List<string> DiffDataList { get; private set; } = new List<string>();

        protected Dictionary<string, (byte[], DateTime)> originAssetDataDic { get; private set; } = new Dictionary<string, (byte[], DateTime)>();

        private int _inProgressOperationCount = 0;
        private List<string> _keysToRemove = new List<string>();
        private CancellationTokenSource _onUpdateToken = null;

        protected override void Initialized()
        {
            base.Initialized();
            DontDestroyOnLoad(this);

            IsSetup = false;
            IsServer = false;

            OnResetSetup();
            OnUnloadAssetBundles();
        }

        protected override void OnDestroy()
        {
            OnDisposeUpdate();
            base.OnDestroy();
        }

        #region Path

        /// <summary>プラットフォーム対応フォルダ名</summary>
        public static string GetPlatformDirectoryName(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";

                case RuntimePlatform.Android:
                    return "Android";

                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "OSX";

                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";

#if !UNITY_5_4_OR_NEWER
                        case RuntimePlatform.OSXWebPlayer:
                        case RuntimePlatform.WindowsWebPlayer:
                            return "WebPlayer";
#endif
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";

                default:
                    break;
            }

            return string.Empty;
        }

        /// <summary>Manifestファイル名(プラットフォーム別)</summary>
        public static string GetPlatformManifestName(RuntimePlatform platform)
            => $"{GetPlatformDirectoryName(platform)}";

#if UNITY_EDITOR

        public static RuntimePlatform ToRuntimePlatform(BuildTarget buildPlatform)
        {
            switch (buildPlatform)
            {
                case BuildTarget.iOS:
                    return RuntimePlatform.IPhonePlayer;

                case BuildTarget.Android:
                    return RuntimePlatform.Android;

                case BuildTarget.StandaloneOSX:
                    return RuntimePlatform.OSXPlayer;

#if !UNITY_5_4_OR_NEWER
                    case BuildTarget.WebPlayer:
                        return Application.platform;
#endif

                case BuildTarget.WebGL:
                    return RuntimePlatform.WebGLPlayer;

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                default:
                    break;
            }

            return RuntimePlatform.WindowsPlayer;
        }

        /// <summary>プラットフォーム対応フォルダ名</summary>
        public static string GetPlatformDirectoryName(BuildTarget buildPlatform)
            => GetPlatformDirectoryName(ToRuntimePlatform(buildPlatform));

        /// <summary>出力先ディレクトリ(プラットフォーム別)</summary>
        public static string GetPlatformManifestName(BuildTarget buildPlatform)
            => GetPlatformManifestName(ToRuntimePlatform(buildPlatform));
#endif

        #endregion

        protected string OnGetAssetDirectoryPath(RuntimePlatform platform)
        {
            return $"{DirectoryPath}/{LocalPath}/{GetPlatformDirectoryName(platform)}";
        }

        private async UniTask<(byte[], DateTime)> OnReadFileAsync(CancellationToken token, string filePath)
        {
            Log(DebugerLogType.Info, "OnReadFileAsync", $"filePath={filePath}");

            FileStream fileStream = null;

            try
            {
                if (!File.Exists(filePath))
                    throw new Exception("not found file.");

                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fileStream == null)
                    throw new Exception("create fileStream error.");

                var result = new byte[(int)fileStream.Length];

                var waitTimeoutToken = new CancellationTokenSource();
                waitTimeoutToken.CancelAfterSlim(TimeSpan.FromMilliseconds(readTimeoutMillisecond));
                var waitLinkedToken = CancellationTokenSource.CreateLinkedTokenSource(token, waitTimeoutToken.Token);

                await fileStream.ReadAsync(result, 0, result.Length, waitLinkedToken.Token);

                token.ThrowIfCancellationRequested();

                // 更新日を取得
                var fileInfo = new FileInfo(filePath);

                return (result, fileInfo != null ? fileInfo.LastWriteTimeUtc : DateTime.Now);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnReadFileAsync", $"path={filePath}\n\r{ex.Message}");

                fileStream?.Dispose();

                return (null, DateTime.UtcNow);
            }
            finally
            {
                fileStream?.Close();
            }
        }

        private async UniTask<bool> OnDeleteDirectoryAsync(CancellationToken token, string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    throw new Exception("data null or zero.");

                Directory.Delete(path, true);

                var waitTimeoutToken = new CancellationTokenSource();
                waitTimeoutToken.CancelAfterSlim(TimeSpan.FromMilliseconds(deleteWaitTimeoutMillisecond));
                var waitLinkedToken = CancellationTokenSource.CreateLinkedTokenSource(token, waitTimeoutToken.Token);

                // 消えるのを待つ
                await UniTask.WaitUntil(() => !Directory.Exists(path), PlayerLoopTiming.Update, waitLinkedToken.Token);

                return true;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnDeleteDirectoryAsync", $"<color=red>error</color>. {ex.Message}\r\npath={path}");

                return false;
            }
        }

        private void OnResetSetup()
        {
            OnDisposeUpdate();

            try
            {
                HashList = null;
                originHashList_Win = default;
                originHashList_OSX = default;
                originHashList_iOS = default;
                originHashList_And = default;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnResetSetup", $"{ex.Message}");
            }
            finally
            {
                IsSetup = false;
            }
        }

        /// <summary></summary>
        public async UniTask<bool> SetupAsync(CancellationToken token, bool isServer, string directoryPath)
        {
            IsServer = isServer;

            DirectoryPath = string.IsNullOrEmpty(directoryPath) ? Application.persistentDataPath : directoryPath;

            Log(DebugerLogType.Info, "SetupAsync", $"isServer={IsServer}, isSimulateMode={IsSimulateMode}, isEncrypt={isEncrypt}\n\rdirectoryPath={DirectoryPath}");

            OnResetSetup();

            try
            {
                await LoadHashListAsync(token);

                await LoadAsync(token);

                OnDisposeUpdate();
                _onUpdateToken = new CancellationTokenSource();
                OnUpdateAsync(_onUpdateToken.Token).Forget();

                IsSetup = true;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "SetupAsync", $"{ex.Message}");
                IsSetup = false;
            }

            return IsSetup;
        }

        /// <summary></summary>
        public async UniTask<bool> LoadHashListAsync(CancellationToken token)
        {
            Log(DebugerLogType.Info, "LoadHashListAsync", "");

            var failed = false;

            try
            {
                originHashList_Win = await OnReadFileAsync(token, $"{OnGetAssetDirectoryPath(RuntimePlatform.WindowsPlayer)}/{HashFileName}.{HashFileExtension}");
                originHashList_OSX = await OnReadFileAsync(token, $"{OnGetAssetDirectoryPath(RuntimePlatform.OSXPlayer)}/{HashFileName}.{HashFileExtension}");
                originHashList_iOS = await OnReadFileAsync(token, $"{OnGetAssetDirectoryPath(RuntimePlatform.IPhonePlayer)}/{HashFileName}.{HashFileExtension}");
                originHashList_And = await OnReadFileAsync(token, $"{OnGetAssetDirectoryPath(RuntimePlatform.Android)}/{HashFileName}.{HashFileExtension}");
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "LoadHashListAsync(Hash)", $"{ex.Message}");
                failed = true;
            }

            if (OriginHashList_Active.Item1 != null && OriginHashList_Active.Item1.Length > 0)
            {
                HashList = new AssetBundleHashList();
                HashList.Deserialize(OriginHashList_Active.Item1);
            }

            return (!failed);
        }

        private void OnUnloadAssetBundles()
        {
            try
            {
                AssetBundle.UnloadAllAssetBundles(true);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnUnloadAll", $"{ex.Message}");
            }
            finally
            {
                DiffDataList?.Clear();
                originAssetDataDic?.Clear();

                IsInit = false;
                IsDiffData = false;
            }
        }

        /// <summary></summary>
        public async UniTask<bool> LoadAsync(CancellationToken token)
        {
            Log(DebugerLogType.Info, "LoadAsync", $"directoryPath={DirectoryPath}");

            OnClientRequest();
            OnUnloadAssetBundles();

            try
            {
                if (HashList == null)
                    throw new Exception("not load HashList.");

                var failed = false;

                if (!await OnLoadAssetBundleManifestAsync(token))
                    throw new Exception("manifest load error.");

                if (IsServer)
                {
                    await OnLoadOriginData(token);
                }
                else
                {
                    await FixedDifference(token);
                }

                IsInit = !failed;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnLoadDBAsync", $"error. {ex.Message}");
                IsInit = false;
            }

            UpdateDebugDiffList();
            return IsInit;
        }

        /// <summary></summary>
        public (byte[], DateTime) GetHashFileData(RuntimePlatform platform)
        {
            if (singleton != null)
            {
                if (platform == RuntimePlatform.WindowsEditor ||
                    platform == RuntimePlatform.WindowsPlayer)
                    return originHashList_Win;

                if (platform == RuntimePlatform.OSXEditor ||
                    platform == RuntimePlatform.OSXPlayer)
                    return originHashList_OSX;

                if (platform == RuntimePlatform.IPhonePlayer) return originHashList_iOS;
                if (platform == RuntimePlatform.Android) return originHashList_And;
            }
            return (null, DateTime.MinValue);
        }

        private void OnDisposeUpdate()
        {
            _onUpdateToken?.Cancel();
            _onUpdateToken?.Dispose();
            _onUpdateToken = null;
        }

        private async UniTask OnUpdateAsync(CancellationToken token)
        {
            while (_onUpdateToken != null)
            {
                if (IsInit)
                {
                    UpdateLocalLoading();

                    for (_inProgressOperationCount = 0; _inProgressOperationCount < inProgressOperations.Count;)
                    {
                        if (!inProgressOperations[_inProgressOperationCount].Update())
                        {
                            inProgressOperations.RemoveAt(_inProgressOperationCount);
                        }
                        else
                        {
                            _inProgressOperationCount++;
                        }
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                token.ThrowIfCancellationRequested();
            }

            OnDisposeUpdate();
        }

        private void UpdateLocalLoading()
        {
            ResetProgress();

            _keysToRemove.Clear();
            foreach (var load in loadingStreamingAssets)
            {
                if (load.Value == null)
                {
                    _keysToRemove.Add(load.Key);

                    Log(DebugerLogType.Error, "UpdateLocalLoading", $"LoadingStreamingAsset({load.Key}) is value null.");
                    continue;
                }

                Log(DebugerLogType.Info, "UpdateLocalLoading", $"LoadingStreamingAsset({load.Value.AssetName}) Progress={(load.Value.Progress * 100):0.0}%");

                SetProgress(load.Value.Progress);
                SetProgressStatus(load.Value.AssetName);

                if (!load.Value.IsDone && load.Value.EndTimeout < Time.time)
                {
                    load.Value.Dispose();
                    if (!load.Value.Retry())
                    {
                        _keysToRemove.Add(load.Key);
                        Log(DebugerLogType.Error, "UpdateLocalLoading", $"LoadingStreamingAsset({load.Value.AssetName}) is load timeout! [{load.Value.RetryCount}]");

                        continue;
                    }
                }

                if (load.Value.IsDone)
                {
                    if (load.Value.AssetBundle != null && string.IsNullOrEmpty(load.Value.ErrorMessage))
                    {
                        switch (IsDependencies(load.Value.Dependencies))
                        {
                            case DependencyLoad.NoTargets:
                            case DependencyLoad.Success:
                                {
                                    Log(DebugerLogType.Info, "UpdateLocalLoading", $"LoadingStreamingAsset({load.Value.AssetName}) is Loaded.");

                                    loadedAssetBundles.Add(load.Key, new LoadedAssetBundle(load.Value.AssetBundle));
                                    _keysToRemove.Add(load.Key);
                                }
                                break;

                            case DependencyLoad.LoadWait:
                                {
                                    Log(DebugerLogType.Info, "UpdateLocalLoading", $"LoadingStreamingAsset({load.Value.AssetName}) is dependencies load wait. - {load.Value.StrDependencies}");
                                }
                                break;

                            case DependencyLoad.Failed:
                            default:
                                {
                                    loadingStreamingAssetsErrors.Add(load.Value.AssetName, $"{load.Value.AssetName} is not a valid asset bundle. Dependencies load failed. - {load.Value.StrDependencies}");
                                    _keysToRemove.Add(load.Key);

                                    load.Value.AssetBundle?.Unload(false);

                                    Log(DebugerLogType.Error, "UpdateLocalLoading", $"{load.Value.AssetName} is not a valid asset bundle. Dependencies load failed. - {load.Value.StrDependencies}");
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (load.Value.Retry())
                        {
                            Log(DebugerLogType.Error, "UpdateLocalLoading", $"LoadingStreamingAsset({load.Value.AssetName}) is load retry. [{load.Value.RetryCount}]");
                        }
                        else
                        {
                            loadingStreamingAssetsErrors.Add(load.Value.AssetName, $"{load.Value.AssetName} is not a valid asset bundle. [{load.Value.RetryCount}] {load.Value.ErrorMessage}");

                            if (load.Value.IsFileError) assetBundleFileErrors.Add(load.Value.AssetName, load.Value.ErrorMessage);

                            _keysToRemove.Add(load.Key);

                            load.Value.AssetBundle?.Unload(false);

                            Log(DebugerLogType.Error, "UpdateLocalLoading", $"LoadingStreamingAsset({load.Value.AssetName}) is not a valid asset bundle. [{load.Value.RetryCount}] - {load.Value.ErrorMessage}");
                        }
                    }
                }
            }

            foreach (var key in _keysToRemove)
            {
                loadingStreamingAssets[key]?.Dispose();
                loadingStreamingAssets.Remove(key);
            }
        }

        /// <summary></summary>
        public LoadedAssetBundle GetLoadedAssetBundle(string assetBundleName, out string error)
        {
            if (!IsSetup)
            {
                error = "AssetBundleManager instance Error.";
                return null;
            }

            if (loadingStreamingAssetsErrors.TryGetValue(assetBundleName, out error))
                return null;

            LoadedAssetBundle bundle = null;
            loadedAssetBundles.TryGetValue(assetBundleName, out bundle);
            if (bundle == null)
                return null;

            string[] dependencies = null;
            if (!_dependencies.TryGetValue(assetBundleName, out dependencies))
                return bundle;

            for (int i = 0; i < dependencies.Length; i++)
            {
                if (loadingStreamingAssetsErrors.TryGetValue(dependencies[i], out error) ||
                    assetBundleFileErrors.TryGetValue(dependencies[i], out error))
                    return bundle;

                LoadedAssetBundle dependentBundle = null;
                loadedAssetBundles.TryGetValue(dependencies[i], out dependentBundle);
                if (dependentBundle == null)
                    return null;
            }
            return bundle;
        }

        private void OnClientRequest()
        {
            OnClearLoadedAssetBundles();

            loadingStreamingAssets?.Clear();
            loadingStreamingAssetsErrors?.Clear();
            assetBundleFileErrors?.Clear();
            inProgressOperations?.Clear();
            _dependencies?.Clear();
        }

        private void ClearTempList()
        {
            if (inProgressOperations.Count > 0)
                return;

            _dependencies?.Clear();
            loadingStreamingAssetsErrors?.Clear();
        }

        private void OnClearLoadedAssetBundles()
        {
            if (loadedAssetBundles == null) return;

            foreach (var loadAsset in loadedAssetBundles)
            {
                try
                {
                    loadAsset.Value.AssetBundle?.Unload(false);
                }
                catch (Exception ex)
                {
                    Log(DebugerLogType.Info, "OnClearLoadedAssetBundles", $"AssetBundle({loadAsset.Key}) is missing object. {ex.Message}");
                }
            }

            loadedAssetBundles?.Clear();
        }

        private async UniTask<bool> OnLoadAssetBundleManifestAsync(CancellationToken token)
        {
            if (IsSimulateMode) return true;

            var path = $"{AssetDirectoryPath}/{ManifestName}.{ManifestFileExtension}";

            try
            {
                if (HashList == null)
                    throw new Exception("not load assetBundleHash.");

                if (!File.Exists(path))
                    throw new Exception("not found manifest.");

                LoadAssetBundle(ManifestName, true);

                var operation = new AssetBundleLoadManifestOperation(ManifestName, "AssetBundleManifest", typeof(AssetBundleManifest), null);

                if (operation == null)
                    throw new Exception("manifest operation error.");

                var operationTimeoutToken = new CancellationTokenSource();
                operationTimeoutToken.CancelAfterSlim(TimeSpan.FromMilliseconds(LoadingTimeOutMilliseconds));
                var operationLinkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, operationTimeoutToken.Token);

                await operation.ToUniTask(cancellationToken: operationTimeoutToken.Token);

                if (!operation.IsDone())
                    throw new Exception($"manifest operation error. {operation.Error}");

                return true;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnLoadAssetBundleManifestAsync", $"{ex.Message}\r\npath={path}");
                return false;
            }
        }

        protected void LoadAssetBundle(string assetBundleName, bool isLoadingAssetBundleManifest = false)
        {
            Log(DebugerLogType.Info, "LoadAssetBundle", "Loading Asset Bundle " + (isLoadingAssetBundleManifest ? "Manifest: " : ": ") + assetBundleName);

            if (IsSimulateMode) return;

            if (!isLoadingAssetBundleManifest)
            {
                if (Manifest == null)
                {
                    Log(DebugerLogType.Error, "LoadAssetBundle", "Please initialize AssetBundleManifest by calling AssetBundleManager.Initialize()");
                    return;
                }
            }

            var isAlreadyProcessed = LoadAssetBundleInternal(assetBundleName, isLoadingAssetBundleManifest);

            if (!isAlreadyProcessed && !isLoadingAssetBundleManifest)
                LoadDependencies(assetBundleName);
        }

        private string RemapVariantName(string assetBundleName)
        {
            var bundlesWithVariant = Manifest != null ? Manifest.GetAllAssetBundlesWithVariant() : new string[0];

            var split = assetBundleName.Split('.');

            var bestFit = int.MaxValue;
            var bestFitIndex = -1;
            for (int i = 0; i < bundlesWithVariant.Length; i++)
            {
                var curSplit = bundlesWithVariant[i].Split('.');
                if (curSplit[0] != split[0])
                    continue;

                var found = System.Array.IndexOf(activeVariants, curSplit[1]);

                if (found == -1)
                    found = int.MaxValue - 1;

                if (found < bestFit)
                {
                    bestFit = found;
                    bestFitIndex = i;
                }
            }

            if (bestFit == int.MaxValue - 1)
            {
                Log(DebugerLogType.Info, "RemapVariantName", $"Ambiguous asset bundle variant chosen because there was no matching active variant: {bundlesWithVariant[bestFitIndex]}");
            }

            if (bestFitIndex != -1)
            {
                return bundlesWithVariant[bestFitIndex];
            }
            else
            {
                return assetBundleName;
            }
        }

        private bool LoadAssetBundleInternal(string assetBundleName, bool isLoadingAssetBundleManifest)
        {
            LoadedAssetBundle bundle = null;
            loadedAssetBundles.TryGetValue(assetBundleName, out bundle);
            if (bundle != null)
            {
                bundle.ReferencedCount++;
                return true;
            }

            return LocalLoadAssetBundleInternal(assetBundleName, isLoadingAssetBundleManifest);
        }

        private bool LocalLoadAssetBundleInternal(string assetBundleName, bool isLoadingAssetBundleManifest)
        {
            if (loadingStreamingAssets.ContainsKey(assetBundleName))
                return true;

            var bundlePath = $"{AssetDirectoryPath}/{assetBundleName}";

            if (File.Exists(bundlePath))
            {
                var info = HashList != null ? HashList.GetAsset(assetBundleName) : default;

                if (isLoadingAssetBundleManifest)
                {
                    var fs = new FileStream(bundlePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (fs != null && fs.Length > 0)
                    {
                        Log(DebugerLogType.Info, "LoadAssetBundleInternal", $"Manifest({assetBundleName}) data.Length= {fs.Length} bundlePath= {bundlePath}");

                        if (isEncrypt)
                        {
                            var uniqueSalt = System.Text.Encoding.UTF8.GetBytes(assetBundleName);
                            var uncryptor = new SeekableAesStream(fs, encryptKey, uniqueSalt);
                            var load = AssetBundle.LoadFromStream(uncryptor, info.AssetCRC);
                            if (load != null)
                            {
                                Manifest = load.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                            }
                        }
                        else
                        {
                            var load = AssetBundle.LoadFromStream(fs, info.AssetCRC);
                            if (load != null)
                            {
                                Manifest = load.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                            }
                        }

                        fs?.Close();

                        if (Manifest != null)
                        {
                            Log(DebugerLogType.Info, "LoadAssetBundleInternal", $"Manifest({assetBundleName}) is Loaded.");
                            return true;
                        }
                    }
                    else
                    {
                        Log(DebugerLogType.Error, "LoadAssetBundleInternal", $"Manifest({assetBundleName}) Not found file. bundlePath= {bundlePath}");
                    }
                }
                else
                {
                    var dependencies = Manifest.GetAllDependencies(assetBundleName);
                    var load = AssetBundleRequester.CreateRequest(assetBundleName, bundlePath, info.AssetCRC, isEncrypt, encryptKey, dependencies);
                    if (load != null)
                    {
                        loadingStreamingAssets.Add(assetBundleName, load);
                        Log(DebugerLogType.Info, "LoadAssetBundleInternal", $"AssetBundleUniTaskCreateRequest({assetBundleName})  loadPath= {load.Path}");
                    }
                    else
                    {
                        loadingStreamingAssetsErrors.Add(assetBundleName, $"({assetBundleName}) Not found file. bundlePath= {bundlePath}");
                        Log(DebugerLogType.Error, "LoadAssetBundleInternal", $"({assetBundleName}) Not found file. bundlePath= {bundlePath}");
                    }
                }
            }
            else
            {
                Log(DebugerLogType.Error, "LoadAssetBundleInternal", $"({assetBundleName}) Not found file. bundlePath= {bundlePath}");
            }

            return false;
        }

        private DependencyLoad IsDependencies(params string[] dependencies)
        {
            if (dependencies == null || dependencies.Length <= 0)
                return DependencyLoad.NoTargets;

            foreach (var assetName in dependencies)
            {
                if (loadingStreamingAssetsErrors.ContainsKey(assetName) ||
                    assetBundleFileErrors.ContainsKey(assetName))
                    return DependencyLoad.Failed;

                if (!loadedAssetBundles.ContainsKey(assetName))
                    return DependencyLoad.LoadWait;
            }

            return DependencyLoad.Success;
        }

        private void LoadDependencies(string assetBundleName)
        {
            if (Manifest == null)
            {
                Log(DebugerLogType.Error, "LoadDependencies", "Please initialize AssetBundleManifest by calling AssetBundleManager.Initialize()");
                return;
            }

            var dependencies = Manifest.GetAllDependencies(assetBundleName);
            if (dependencies.Length == 0)
                return;

            for (int i = 0; i < dependencies.Length; i++)
            {
                dependencies[i] = RemapVariantName(dependencies[i]);
            }

            _dependencies.Add(assetBundleName, dependencies);
            for (int i = 0; i < dependencies.Length; i++)
            {
                LoadAssetBundleInternal(dependencies[i], false);
            }
        }

        /// <summary></summary>
        public void UnloadAssetBundle(string assetBundleName)
        {
            if (IsSimulateMode) return;

            Log(DebugerLogType.Info, "UnloadAssetBundle", $"{loadedAssetBundles.Count} assetBundle(s) in memory before unloading {assetBundleName}");

            if (string.IsNullOrEmpty(assetBundleName))
            {
                AllUnloadAssetBundle();
            }
            else
            {
                UnloadAssetBundleInternal(assetBundleName);
                UnloadDependencies(assetBundleName);
            }

            Log(DebugerLogType.Info, "UnloadAssetBundle", $"{loadedAssetBundles.Count} assetBundle(s) in memory after unloading {assetBundleName}");
        }

        private void AllUnloadAssetBundle()
        {
            foreach (var loadedAssetBundle in loadedAssetBundles)
            {
                UnloadDependencies(loadedAssetBundle.Key);
            }
        }

        private void UnloadDependencies(string assetBundleName)
        {
            string[] dependencies = null;
            if (!_dependencies.TryGetValue(assetBundleName, out dependencies))
                return;

            for (int i = 0; i < dependencies.Length; i++)
            {
                UnloadAssetBundleInternal(dependencies[i]);
            }

            _dependencies.Remove(assetBundleName);
        }

        private void UnloadAssetBundleInternal(string assetBundleName)
        {
            var error = string.Empty;
            var bundle = GetLoadedAssetBundle(assetBundleName, out error);
            if (bundle == null)
                return;

            if (--bundle.ReferencedCount == 0)
            {
                bundle.AssetBundle.Unload(false);
                loadedAssetBundles.Remove(assetBundleName);

                Log(DebugerLogType.Info, "UnloadAssetBundleInternal", $"{assetBundleName} has been unloaded successfully");
            }
        }

        /// <summary></summary>
        public AssetBundleLoadAssetOperation LoadAssetAsync(string assetBundleName, string assetName, System.Type type, IProgress<float> progress)
        {
            Log(DebugerLogType.Info, "LoadAssetAsync", $"Loading {assetName} from {assetBundleName} bundle");

            ClearTempList();

            AssetBundleLoadAssetOperation operation = null;
#if UNITY_EDITOR
            if (IsSimulateMode)
            {
                var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
                if (assetPaths.Length == 0)
                {
                    Log(DebugerLogType.Error, "LoadAssetAsync", $"There is no asset with name \"{assetName}\" in {assetBundleName}");
                    return null;
                }

                // @TODO: Now we only get the main object from the first asset. Should consider type also.
                var target = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPaths[0]);
                operation = new AssetBundleLoadAssetOperationSimulation(assetBundleName, assetName, target, progress);
            }
            else
#endif
            if (IsInit)
            {
                assetBundleName = RemapVariantName(assetBundleName);
                LoadAssetBundle(assetBundleName);
                operation = new AssetBundleLoadAssetOperationFull(assetBundleName, assetName, type, progress);

                Log(DebugerLogType.Info, "OnLoadAssetAsync", assetBundleName);

                inProgressOperations.Add(operation);
            }

            return operation;
        }

        /// <summary></summary>
        public AssetBundleLoadOperation LoadLevelAsync(string assetBundleName, string levelName, bool isAdditive)
        {
            Log(DebugerLogType.Info, "LoadLevelAsync", $"Loading {levelName} from {assetBundleName} bundle");

            ClearTempList();

            AssetBundleLoadOperation operation = null;
#if UNITY_EDITOR
            if (IsSimulateMode)
            {
                operation = new AssetBundleLoadLevelSimulationOperation(assetBundleName, levelName, isAdditive);
            }
            else
#endif
            if (IsInit)
            {
                assetBundleName = RemapVariantName(assetBundleName);
                LoadAssetBundle(assetBundleName);
                operation = new AssetBundleLoadLevelOperation(assetBundleName, levelName, isAdditive);

                inProgressOperations.Add(operation);
            }

            return operation;
        }

        private async UniTask OnLoadOriginData(CancellationToken token)
        {
            (byte[], DateTime) result = default;

            if (HashList != null && HashList.Assets != null && HashList.Assets.Count > 0)
            {
                foreach (var asset in HashList.Assets)
                {
                    if (string.IsNullOrEmpty(asset.Key)) continue;

                    if (!string.IsNullOrEmpty(asset.Value.AssetName))
                    {
                        result = await OnReadFileAsync(token, $"{AssetDirectoryPath}/{asset.Value.AssetName}");
                        originAssetDataDic.Add($"{asset.Value.AssetName}", result);
                    }

                    if (!string.IsNullOrEmpty(asset.Value.ManifestName))
                    {
                        result = await OnReadFileAsync(token, $"{AssetDirectoryPath}/{asset.Value.ManifestName}.{ManifestFileExtension}");
                        originAssetDataDic.Add($"{asset.Value.ManifestName}.{ManifestFileExtension}", result);
                    }
                }
            }
        }

        /// <summary></summary>
        public async UniTask<bool> FixedDifference(CancellationToken token)
        {
            Log(DebugerLogType.Info, "FixedDifference", "");

            IsDiffData = false;
            DiffDataList.Clear();

            try
            {
                if (HashList == null || HashList.Assets == null || HashList.Assets.Count <= 0)
                    throw new Exception("not found master data.");

                (byte[], DateTime) result = default;

                result = await OnReadFileAsync(token, $"{AssetDirectoryPath}/{HashList.PlatformMaster.ManifestName}");
                CheckSetDifferenceList(HashList.PlatformMaster.ManifestName, HashList.PlatformMaster.ManifestSize, result.Item1);

                result = await OnReadFileAsync(token, $"{AssetDirectoryPath}/{HashList.PlatformMaster.AssetName}");
                CheckSetDifferenceList(HashList.PlatformMaster.AssetName, HashList.PlatformMaster.AssetSize, result.Item1);

                foreach (var asset in HashList.Assets.Values)
                {
                    if (string.IsNullOrEmpty(asset.AssetName)) continue;

                    result = await OnReadFileAsync(token, $"{AssetDirectoryPath}/{asset.AssetName}");
                    CheckSetDifferenceList(asset.AssetName, asset.AssetSize, result.Item1);

                    result = await OnReadFileAsync(token, $"{AssetDirectoryPath}/{asset.ManifestName}");
                    CheckSetDifferenceList(asset.ManifestName, asset.ManifestSize, result.Item1);
                }

                IsDiffData = (DiffDataList.Count > 0);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "FixedDifference", $"{ex.Message}");
                IsDiffData = true;
            }

            UpdateDebugDiffList();
            return IsDiffData;
        }

        private void CheckSetDifferenceList(string fileName, int targetSize, byte[] result)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            if (result != null)
            {
                if (result.Length > 0 && result.Length == targetSize)
                    return;
            }

            Log(DebugerLogType.Info, "CheckSetDifferenceList", $"Diff:{fileName}|{targetSize}⇔{(result != null ? result.Length : 0)}");
            DiffDataList.Add(fileName);
        }

        /// <summary></summary>
        public async UniTask<(byte[], DateTime)> GetAssetDataBinary(CancellationToken token, RuntimePlatform platform, string fileName)
        {
            if (platform == activePlatform)
            {
                if (originAssetDataDic.ContainsKey(fileName))
                    return originAssetDataDic[fileName];
            }
            return await OnReadFileAsync(token, $"{OnGetAssetDirectoryPath(platform)}/{fileName}");
        }

        public async UniTask<string> ClearCacheAsync(CancellationToken token)
        {
            try
            {
                if (IsServer)
                    throw new Exception("※ServerではClearCacheできません");

                // 処理を全て止めて初期化してから消す
                OnResetSetup();
                OnUnloadAssetBundles();

                if (!await OnDeleteDirectoryAsync(token, $"{DirectoryPath}/{LocalPath}"))
                    throw new Exception($"delete failed. path={DirectoryPath}/{LocalPath}");

                return string.Empty;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "ClearCacheAsync", $"{ex.Message}");
                return ex.Message;
            }
        }

        #region DEBUG

#if UNITY_EDITOR && DEBUG
        private string[] _debugOriginAssetDataList = null;
#endif

        private void UpdateDebugDiffList()
        {
#if UNITY_EDITOR && DEBUG
            _debugOriginAssetDataList = originAssetDataDic.Keys.ToArray();
#endif
        }

        #endregion
    }
}
