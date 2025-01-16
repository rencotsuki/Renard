#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS0436

using System;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renard
{
    using Debuger;
    using AssetBundleUniTask;

    public static class AssetBundleBuildScript
    {
#if UNITY_EDITOR

        private static bool isDebugLog => AssetBundleManager.IsDebugLogMaster;
        private static string hashFileName => AssetBundleManager.HashFileName;
        private static string hashFileExtension => AssetBundleManager.HashFileExtension;

        private static string GetPlatformDirectoryName(BuildTarget buildTarget)
            => AssetBundleManager.GetPlatformDirectoryName(buildTarget);

        private static string GetPlatformManifestName(BuildTarget buildTarget)
            => AssetBundleManager.GetPlatformManifestName(buildTarget);

        private static void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!isDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(typeof(AssetBundleBuildScript), logType, methodName, message);
        }

        public static void BuildAssetBundles(string outputDirectory, bool isEncrypt, string encryptKey = "", bool isViewStatusBar = false)
            => BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget, outputDirectory, isEncrypt, encryptKey, isViewStatusBar);

        public static void BuildAssetBundles(BuildTarget buildTarget, string outputDirectory, bool isEncrypt, string encryptKey, bool isViewStatusBar = false)
        {
            var outputPath = $"{outputDirectory}/{GetPlatformDirectoryName(buildTarget)}";
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            var manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, buildTarget);

            CreateAssetBundleHashList(outputPath, buildTarget, manifest, isViewStatusBar);

            if (isEncrypt)
                AssetBundleEncryptionAndCreateHashList(outputPath, buildTarget, manifest, encryptKey, isViewStatusBar);

            if (isViewStatusBar)
                EditorUtility.ClearProgressBar();
        }

        private static AssetBundleHashList OnLoadAssetBundleHashList(string path, string fileName)
        {
            try
            {
                var result = new AssetBundleHashList();
                if (File.Exists($"{path}/{fileName}.{AssetBundleManager.HashFileExtension}"))
                {
                    // TODO: UTF8のBomに注意！！
                    var data = File.ReadAllText($"{path}/{fileName}.{AssetBundleManager.HashFileExtension}", new UTF8Encoding(false));

                    if (data != null && data.Length > 0)
                        result.Deserialize(data);
                }

                return result;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnLoadAssetBundleHashList", $"<color=red>Failed</color>. {ex.Message}");
                return null;
            }
        }

        private static bool OnSaveAssetBundleHashList(AssetBundleHashList data, string outputPath, string fileName)
        {
            try
            {
                if (data == null)
                    throw new Exception($"AssetBundleHashList null. path={outputPath}/{fileName}");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                // TODO: UTF8のBomに注意！！
                File.WriteAllText($"{outputPath}/{fileName}", data.Serialize(), new UTF8Encoding(false));
                return true;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnSaveAssetBundleHashList", $"<color=red>Failed</color>. {ex.Message}");
                return false;
            }
        }

        private static void CreateAssetBundleHashList(string basePath, BuildTarget buildTarget, AssetBundleManifest manifest, bool isViewStatusBar)
        {
            if (manifest == null)
            {
                Log(DebugerLogType.Info, "CreateAssetBundleHashList", "No assetBundle output manifest.");
                return;
            }

            var assetBundleHashList = OnLoadAssetBundleHashList(basePath, hashFileExtension);
            var assetBundles = manifest.GetAllAssetBundles();

            var manifestName = GetPlatformManifestName(buildTarget);
            if (isViewStatusBar) EditorUtility.DisplayProgressBar("AssetBundleEncryption", $"{manifestName}", 0);

            // AssetBundle Manifest
            {
                var dataSize = File.ReadAllBytes(Path.Combine(basePath, manifestName)).Length;
                if (dataSize <= 0)
                {
                    Log(DebugerLogType.Info, "CreateAssetBundleHashList", $"<color=red>Failed</color>. {manifestName}");
                    return;
                }

                var manifestDataSize = File.ReadAllBytes(Path.Combine(basePath, $"{manifestName}.manifest")).Length; 
                if (manifestDataSize <= 0)
                {
                    Log(DebugerLogType.Info, "CreateAssetBundleHashList", $"<color=red>Failed</color>. {manifestName}.manifest");
                }

                uint crc = 0;
                BuildPipeline.GetCRCForAssetBundle(Path.Combine(basePath, manifestName), out crc);

                assetBundleHashList.PlatformMaster.SetAssetInfo(manifestName, string.Empty, dataSize, crc);
                assetBundleHashList.PlatformMaster.SetManifestInfo(manifestName, manifestDataSize);
            }

            // AssetBundles
            assetBundleHashList.Assets?.Clear();

            for (int i = 0; i < assetBundles.Length; i++)
            {
                if (isViewStatusBar) EditorUtility.DisplayProgressBar("AssetBundleEncryption", $"{assetBundles[i]}", (float)i / (float)(assetBundles.Length + 1));

                var hash = manifest.GetAssetBundleHash(assetBundles[i]);

                var dataSize = File.ReadAllBytes(Path.Combine(basePath, assetBundles[i])).Length;
                if (dataSize <= 0)
                {
                    Log(DebugerLogType.Info, "CreateAssetBundleHashList", $"<color=red>Failed</color>. {assetBundles[i]}");
                }

                var manifestDataSize = File.ReadAllBytes(Path.Combine(basePath, $"{assetBundles[i]}.manifest")).Length;
                if (manifestDataSize <= 0)
                {
                    Log(DebugerLogType.Info, "CreateAssetBundleHashList", $"<color=red>Failed</color>. {assetBundles[i]}.manifest");
                }

                uint crc = 0;
                BuildPipeline.GetCRCForAssetBundle(Path.Combine(basePath, assetBundles[i]), out crc);

                var data = AssetBundleInfo.Create(assetBundles[i], hash.ToString(), dataSize, manifestDataSize, crc);
                assetBundleHashList.Assets.Add(assetBundles[i], data);
            }

            assetBundleHashList.CreateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            OnSaveAssetBundleHashList(assetBundleHashList, basePath, $"{hashFileName}.{hashFileExtension}");

            AssetDatabase.Refresh();
        }

        private static void AssetBundleEncryptionAndCreateHashList(string basePath, BuildTarget buildTarget, AssetBundleManifest manifest, string encryptKey, bool isViewStatusBar)
        {
            if (manifest == null || string.IsNullOrEmpty(encryptKey))
            {
                Log(DebugerLogType.Info, "AssetBundleEncryptionAndCreateHashList", "No assetBundle output manifest.");
                return;
            }

            var outputPath = $"{basePath}/Encrypt";
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);

            Directory.CreateDirectory(outputPath);

            var assetBundleHashList = OnLoadAssetBundleHashList(outputPath, hashFileName);
            var assetBundles = manifest.GetAllAssetBundles();

            var manifestName = GetPlatformManifestName(buildTarget);
            if (isViewStatusBar) EditorUtility.DisplayProgressBar("AssetBundleEncryption", $"{manifestName}", 0);

            // AssetBundle Manifest
            {
                var dataSize = EncryptionAssetBundle(manifestName, Path.Combine(basePath, manifestName), Path.Combine(outputPath, manifestName), encryptKey);
                if (dataSize <= 0)
                {
                    Log(DebugerLogType.Info, "AssetBundleEncryptionAndCreateHashList", $"EncryptionAssetBundle <color=red>Failed</color>. {manifestName}");
                    return;
                }

                var manifestDataSize = EncryptionAssetBundle(manifestName, Path.Combine(basePath, $"{manifestName}.manifest"), Path.Combine(outputPath, $"{manifestName}.manifest"), encryptKey);
                if (manifestDataSize <= 0)
                {
                    Log(DebugerLogType.Info, "AssetBundleEncryptionAndCreateHashList", $"EncryptionAssetBundle <color=red>Failed</color>. {manifestName}");
                }

                uint crc = 0;
                BuildPipeline.GetCRCForAssetBundle(Path.Combine(basePath, manifestName), out crc);

                assetBundleHashList.PlatformMaster.SetAssetInfo(manifestName, string.Empty, dataSize, crc);
                assetBundleHashList.PlatformMaster.SetManifestInfo(manifestName, manifestDataSize);
            }

            // AssetBundles
            assetBundleHashList.Assets?.Clear();

            for (int i = 0; i < assetBundles.Length; i++)
            {
                if (isViewStatusBar) EditorUtility.DisplayProgressBar("AssetBundleEncryption", $"{assetBundles[i]}", (float)i / (float)(assetBundles.Length + 1));

                var hash = manifest.GetAssetBundleHash(assetBundles[i]);

                var dataSize = EncryptionAssetBundle(assetBundles[i], Path.Combine(basePath, assetBundles[i]), Path.Combine(outputPath, assetBundles[i]), encryptKey);
                if (dataSize <= 0)
                {
                    Log(DebugerLogType.Info, "AssetBundleEncryptionAndCreateHashList", $"EncryptionAssetBundle <color=red>Failed</color>. {assetBundles[i]}");
                }

                var manifestDataSize = EncryptionAssetBundle($"{assetBundles[i]}.manifest", Path.Combine(basePath, $"{assetBundles[i]}.manifest"), Path.Combine(outputPath, $"{assetBundles[i]}.manifest"), encryptKey);
                if (manifestDataSize <= 0)
                {
                    Log(DebugerLogType.Info, "AssetBundleEncryptionAndCreateHashList", $"EncryptionAssetBundle <color=red>Failed</color>. {assetBundles[i]}.manifest");
                }

                uint crc = 0;
                BuildPipeline.GetCRCForAssetBundle(Path.Combine(basePath, assetBundles[i]), out crc);

                var data = AssetBundleInfo.Create(assetBundles[i], hash.ToString(), dataSize, manifestDataSize, crc);
                assetBundleHashList.Assets.Add(assetBundles[i], data);
            }

            assetBundleHashList.CreateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            OnSaveAssetBundleHashList(assetBundleHashList, outputPath, $"{hashFileName}.{hashFileExtension}");

            AssetDatabase.Refresh();
        }

        private static int EncryptionAssetBundle(string assetName, string basePath, string outputPath, string encryptKey)
        {
            try
            {
                if (string.IsNullOrEmpty(assetName))
                    throw new Exception("null or empty assetName.");

                if (!File.Exists(basePath))
                    throw new Exception($"not found baseFile. path={basePath}");

                var data = File.ReadAllBytes(basePath);
                if (data.Length <= 0)
                    throw new Exception($"read file error. path={basePath}");

                // 暗号化してファイルに書き込む
                using (var baseStream = new FileStream(outputPath, FileMode.OpenOrCreate))
                {
                    var uniqueSalt = Encoding.UTF8.GetBytes(assetName);
                    var cryptor = new SeekableAesStream(baseStream, encryptKey, uniqueSalt);
                    cryptor.Write(data, 0, data.Length);
                    return data.Length;
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "EncryptionAssetBundle", $"[<color=red>failed</color>] {ex.Message}");
                return 0;
            }
        }

#endif
    }
}