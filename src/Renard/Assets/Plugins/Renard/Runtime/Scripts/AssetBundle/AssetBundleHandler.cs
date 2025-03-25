using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Renard;

public static class AssetBundleHandler
{
    public static int LoadingTimeOutMilliseconds => AssetBundleManager.LoadingTimeOutMilliseconds;
    public static int LoadRetryCount => AssetBundleManager.LoadRetryCount;

    private static AssetBundleManager singleton => AssetBundleManager.Singleton;

    public static bool IsSetup => singleton != null ? singleton.IsSetup : false;

    public static bool IsInit => singleton != null ? singleton.IsInit : false;

    public static bool IsDiffData => singleton != null ? singleton.IsDiffData : false;

    public static List<string> DiffDataList => singleton != null ? singleton.DiffDataList : null;

    public static string ManifestName => singleton != null ? singleton.ManifestName : string.Empty;

    public static string AssetDirectoryPath => singleton != null ? singleton.AssetDirectoryPath : string.Empty;

    public static AssetBundleManifest AssetBundleManifest => singleton != null ? singleton.Manifest : null;

    public static AssetBundleHashList AssetBundleHashList => singleton != null ? singleton.HashList : null;

    public static async UniTask<bool> SetupAsync(CancellationToken token, string directoryPath = "", bool master = false)
         => singleton != null ? await singleton.SetupAsync(token, directoryPath, master) : false;

    public static async UniTask<bool> LoadHashListAsync(CancellationToken token)
        => singleton != null ? await singleton.LoadHashListAsync(token) : false;

    public static (byte[], DateTime) GetHashListBinary()
        => singleton != null ? singleton.OriginHashList_Active : (null, DateTime.UtcNow);

    public static (byte[], DateTime) GetHashListBinary(RuntimePlatform platform)
        => singleton != null ? singleton.GetHashFileData(platform) : (null, DateTime.UtcNow);

    public static async UniTask<bool> LoadAsync(CancellationToken token)
        => singleton != null ? await singleton.LoadAsync(token) : false;

    public static async UniTask<bool> FixedDifference(CancellationToken token)
        => singleton != null ? await singleton.FixedDifference(token) : false;

    public static async UniTask<(byte[], DateTime)> GetAssetDataBinary(CancellationToken token, RuntimePlatform platform, string fileName)
        => singleton != null ? await singleton.GetAssetDataBinary(token, platform, fileName) : (null, DateTime.UtcNow);

    public static async UniTask<string> ClearCacheAsync(CancellationToken token)
        => singleton != null ? await singleton.ClearCacheAsync(token) : "singleton null.";

    public static AssetBundleLoadAssetOperation LoadAssetAsync<T>(string assetBundleName, string assetName, IProgress<float> progress = null)
        => singleton?.LoadAssetAsync(assetBundleName, assetName, typeof(T), progress);

    public static AssetBundleLoadAssetOperation LoadAssetAsync(string assetBundleName, string assetName, IProgress<float> progress = null)
        => LoadAssetAsync<UnityEngine.Object>(assetBundleName, assetName, progress);

    public static AssetBundleLoadOperation LoadLevelAsync(string assetBundleName, string levelName, bool isAdditive)
        => singleton?.LoadLevelAsync(assetBundleName, levelName, isAdditive);

    public static void UnloadAssetBundle(string assetBundleName = "")
        => singleton?.UnloadAssetBundle(assetBundleName);
}
