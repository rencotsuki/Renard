using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renard.AssetBundleUniTask
{
    using Debuger;
    using AssetBundleInfo = AssetBundleHashList.AssetBundleInfo;

    public static class AssetBundleBuildScript
    {
        public const string OutputEncryptPath = "Encrypt";

        private static string hashFileName => AssetBundleManager.HashFileName;
        private static string hashFileExtension => AssetBundleManager.HashFileExtension;

        private static bool _isDebugLog = false;

        private static void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!_isDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(typeof(AssetBundleBuildScript), logType, methodName, message);
        }

#if UNITY_EDITOR

        private const string progressBarTitle = "Create AssetBundle";

        private static bool _isViewStatusBar = false;

        private static void ClearProgressBar()
        {
            if (!_isViewStatusBar)
                return;

            EditorUtility.ClearProgressBar();
        }

        private static void ProgressBar(string title, string message, float value)
        {
            if (!_isViewStatusBar)
                return;

            EditorUtility.DisplayProgressBar(title, message, 0);
        }

        private static string GetPlatformDirectoryName(BuildTarget buildTarget)
            => AssetBundleManager.GetPlatformDirectoryName(buildTarget);

        private static string GetPlatformManifestName(BuildTarget buildTarget)
            => AssetBundleManager.GetPlatformManifestName(buildTarget);

        public static void BuildAssetBundles(string outputDirectory, AssetBundleConfigAsset config, bool isDebugLog = false, bool isViewStatusBar = false)
            => BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget, outputDirectory, config, isDebugLog, isViewStatusBar);

        public static void BuildAssetBundles(BuildTarget buildTarget, string outputDirectory, AssetBundleConfigAsset config, bool isDebugLog = false, bool isViewStatusBar = false)
        {
            _isDebugLog = isDebugLog;
            _isViewStatusBar = isViewStatusBar;

            ClearProgressBar();

            var outputPath = $"{outputDirectory}/{GetPlatformDirectoryName(buildTarget)}";
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            var manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, buildTarget);

            Create(outputPath, buildTarget, manifest);

            if (config != null && config.GetConfig(buildTarget).IsEncrypt)
                CreateEncrypt(outputPath, buildTarget, manifest, config);

            ClearProgressBar();
        }

        private static AssetBundleHashList ReadCreateAssetBundleHashList(string path, string fileName)
        {
            var result = new AssetBundleHashList();

            try
            {
                if (!File.Exists($"{path}/{fileName}.{AssetBundleManager.HashFileExtension}"))
                    throw new Exception($"not found assetBundle hashList. create new file. path={path}/{fileName}.{AssetBundleManager.HashFileExtension}");

                // UTF8のBomに注意!!
                var data = File.ReadAllText($"{path}/{fileName}.{AssetBundleManager.HashFileExtension}", new UTF8Encoding(false));

                if (data != null && data.Length > 0)
                    result.Deserialize(data);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "ReadCreateAssetBundleHashList", $"{ex.Message}");
            }
            return result;
        }

        private static bool WriteAssetBundleHashList(AssetBundleHashList data, string outputPath, string fileName)
        {
            try
            {
                if (data == null)
                    throw new Exception($"null assetBundle hashList. path={outputPath}/{fileName}");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                // UTF8のBomに注意!!
                File.WriteAllText($"{outputPath}/{fileName}", data.Serialize(), new UTF8Encoding(false));
                return true;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "WriteAssetBundleHashList", $"{ex.Message}");
                return false;
            }
        }

        private static void Create(string originPath, BuildTarget buildTarget, AssetBundleManifest manifest)
        {
            try
            {
                if (manifest == null)
                    throw new Exception("null assetBundle manifest.");

                var assetBundleHashList = ReadCreateAssetBundleHashList(originPath, hashFileExtension);
                var assetBundles = manifest.GetAllAssetBundles();
                var manifestName = GetPlatformManifestName(buildTarget);

                (int, int) result = default;
                uint crc = 0;

                // AssetBundle Manifest
                {
                    ProgressBar(progressBarTitle, $"{manifestName}", 0);

                    result = LoadAssetBundleManifest(manifestName, originPath);

                    BuildPipeline.GetCRCForAssetBundle(Path.Combine(originPath, manifestName), out crc);

                    assetBundleHashList.PlatformMaster.SetAssetInfo(manifestName, string.Empty, result.Item1, crc);
                    assetBundleHashList.PlatformMaster.SetManifestInfo(manifestName, result.Item2);
                }

                assetBundleHashList.Assets?.Clear();

                // AssetBundles
                {
                    AssetBundleInfo info = default;
                    Hash128 hash = default;

                    for (int i = 0; i < assetBundles.Length; i++)
                    {
                        ProgressBar(progressBarTitle, $"{assetBundles[i]}", (float)i / (float)(assetBundles.Length + 1));

                        hash = manifest.GetAssetBundleHash(assetBundles[i]);

                        result = LoadAssetBundle(assetBundles[i], originPath);

                        BuildPipeline.GetCRCForAssetBundle(Path.Combine(originPath, assetBundles[i]), out crc);

                        info = AssetBundleInfo.Create(assetBundles[i], hash.ToString(), result.Item1, result.Item2, crc);
                        assetBundleHashList.Assets.Add(assetBundles[i], info);
                    }
                }

                assetBundleHashList.CreateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                // サブモジュールとして検索
                var commitHash = DebugDotGit.GetSubmodulesCommitHash(originPath);
                if (string.IsNullOrEmpty(commitHash))
                {
                    // 取得できていなかったら通常のGitリポジトリとして検索
                    commitHash = DebugDotGit.GetCurrentCommitHash();
                }

                assetBundleHashList.CommitHash = commitHash;

                WriteAssetBundleHashList(assetBundleHashList, originPath, $"{hashFileName}.{hashFileExtension}");

                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "Create", $"{ex.Message}");
            }
        }

        private static (int, int) LoadAssetBundleManifest(string manifestName, string originPath)
        {
            try
            {
                var dataSize = File.ReadAllBytes(Path.Combine(originPath, manifestName)).Length;
                if (dataSize <= 0)
                    throw new Exception($"create failed data file. name={manifestName}");

                var manifestSize = File.ReadAllBytes(Path.Combine(originPath, $"{manifestName}.manifest")).Length;
                if (manifestSize <= 0)
                    throw new Exception($"create failed manifest file. name={manifestName}");

                return (dataSize, manifestSize);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "LoadAssetBundleManifest", $"{ex.Message}");
            }
            return (0, 0);
        }

        private static (int, int) LoadAssetBundle(string assetName, string originPath)
        {
            try
            {
                var dataSize = File.ReadAllBytes(Path.Combine(originPath, assetName)).Length;
                if (dataSize <= 0)
                    throw new Exception($"create failed data file. name={assetName}");

                var manifestSize = File.ReadAllBytes(Path.Combine(originPath, $"{assetName}.manifest")).Length;
                if (manifestSize <= 0)
                    throw new Exception($"create failed manifest file. name={assetName}");

                return (dataSize, manifestSize);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "LoadAssetBundle", $"{ex.Message}");
            }
            return (0, 0);
        }

        //-- 暗号化対応

        private static void CreateEncrypt(string originPath, BuildTarget buildTarget, AssetBundleManifest manifest, AssetBundleConfigAsset config)
        {
            try
            {
                if (manifest == null)
                    throw new Exception("null assetBundle manifest.");

                if (config == null)
                    throw new Exception("null assetBundle config.");

                var outputPath = $"{originPath}/{OutputEncryptPath}";
                if (Directory.Exists(outputPath))
                    Directory.Delete(outputPath, true);

                Directory.CreateDirectory(outputPath);

                var assetBundleHashList = ReadCreateAssetBundleHashList(outputPath, hashFileName);
                var assetBundles = manifest.GetAllAssetBundles();
                var manifestName = GetPlatformManifestName(buildTarget);

                var key = config.EncryptKey;
                var iv = config.EncryptIV;
                (int, int) result = default;
                uint crc = 0;

                // AssetBundle Manifest
                {
                    ProgressBar($"{progressBarTitle}[Encrypt]", $"{manifestName}", 0);

                    result = CreateEncryptAssetBundleManifest(manifestName, originPath, outputPath, key, iv);

                    BuildPipeline.GetCRCForAssetBundle(Path.Combine(originPath, manifestName), out crc);

                    assetBundleHashList.PlatformMaster.SetAssetInfo(manifestName, string.Empty, result.Item1, crc);
                    assetBundleHashList.PlatformMaster.SetManifestInfo(manifestName, result.Item2);
                }

                assetBundleHashList.Assets?.Clear();

                // AssetBundles
                {
                    AssetBundleInfo info = default;
                    Hash128 hash = default;

                    for (int i = 0; i < assetBundles.Length; i++)
                    {
                        ProgressBar($"{progressBarTitle}[Encrypt]", $"{assetBundles[i]}", (float)i / (float)(assetBundles.Length + 1));

                        hash = manifest.GetAssetBundleHash(assetBundles[i]);

                        result = CreateEncryptAssetBundle(assetBundles[i], originPath, outputPath, key, iv);

                        BuildPipeline.GetCRCForAssetBundle(Path.Combine(originPath, assetBundles[i]), out crc);

                        info = AssetBundleInfo.Create(assetBundles[i], hash.ToString(), result.Item1, result.Item2, crc);
                        assetBundleHashList.Assets.Add(assetBundles[i], info);
                    }
                }

                assetBundleHashList.CreateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                WriteAssetBundleHashList(assetBundleHashList, outputPath, $"{hashFileName}.{hashFileExtension}");

                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "CreateEncrypt", $"{ex.Message}");
            }
        }

        private static (int, int) CreateEncryptAssetBundleManifest(string manifestName, string originPath, string outputPath, string key, string iv)
        {
            try
            {
                var dataSize = WriteEncryptFile(manifestName, Path.Combine(originPath, manifestName), Path.Combine(outputPath, manifestName), key, iv);
                if (dataSize <= 0)
                    throw new Exception($"create failed data file. name={manifestName}");

                var manifestSize = WriteEncryptFile(manifestName, Path.Combine(originPath, $"{manifestName}.manifest"), Path.Combine(outputPath, $"{manifestName}.manifest"), key, iv);
                if (manifestSize <= 0)
                    throw new Exception($"create failed manifest file.<color=red>Failed</color>. name={manifestName}");

                return (dataSize, manifestSize);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "CreateEncryptAssetBundleManifest", $"{ex.Message}");
            }
            return (0, 0);
        }

        private static (int, int) CreateEncryptAssetBundle(string assetName, string originPath, string outputPath, string key, string iv)
        {
            try
            {
                var dataSize = WriteEncryptFile(assetName, Path.Combine(originPath, assetName), Path.Combine(outputPath, assetName), key, iv);
                if (dataSize <= 0)
                    throw new Exception($"create failed data file. name={assetName}");

                var manifestSize = WriteEncryptFile($"{assetName}.manifest", Path.Combine(originPath, $"{assetName}.manifest"), Path.Combine(outputPath, $"{assetName}.manifest"), key, iv);
                if (manifestSize <= 0)
                    throw new Exception($"create failed manifest file.<color=red>Failed</color>. name={assetName}");

                return (dataSize, manifestSize);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "CreateEncryptAssetBundle", $"{ex.Message}");
            }
            return (0, 0);
        }

        private static int WriteEncryptFile(string assetName, string originPath, string outputPath, string key, string iv)
        {
            try
            {
                if (string.IsNullOrEmpty(key) || key.Length != AESGenerator.EncryptKeyLength)
                    throw new Exception($"encryptKey error. length={(key != null ? key.Length : 0)}");

                if (string.IsNullOrEmpty(iv) || iv.Length != AESGenerator.EncryptIVLength)
                    throw new Exception($"encryptIV error. length={(iv != null ? iv.Length : 0)}");

                if (!File.Exists(originPath))
                    throw new Exception($"not found originFile. path={originPath}");

                var data = File.ReadAllBytes(originPath);
                if (data.Length <= 0)
                    throw new Exception($"read file error. path={originPath}");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                using (var fs = new FileStream(outputPath, FileMode.OpenOrCreate))
                {
                    var cryptor = new SeekableAesStream(fs, key, iv, _isDebugLog);
                    cryptor.Write(data, 0, data.Length);
                    return data.Length;
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "WriteEncryptFile", $"[<color=red>failed</color>] {ex.Message}");
                return 0;
            }
        }

        //-- エディタ再生用の複製処理

        public static bool CopyAssets(BuildTarget buildTarget, bool isDebugLog = false, bool isViewStatusBar = false)
        {
            _isDebugLog = isDebugLog;
            _isViewStatusBar = isViewStatusBar;

            ClearProgressBar();

            try
            {
                var platform = AssetBundleManager.ToRuntimePlatform(buildTarget);

                var assetBundleConfig = AssetBundleConfigAsset.Load();
                var config = assetBundleConfig?.GetConfig(platform);
                if (config == null)
                    throw new Exception("not found assetBundleConfig.");

                var originPath = $"{assetBundleConfig.OutputFullPath}/{AssetBundleManager.GetPlatformDirectoryName(platform)}";

                if (!Directory.Exists(originPath))
                    throw new Exception($"not found create assetBundle. path={originPath}");

                var targetPath = $"{Application.persistentDataPath}/{config.LocalPath}";

                // ファイル複製（再帰的）
                OnCopyFile(originPath, targetPath);

                return true;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "CopyAssets", $"{ex.Message}");
                return false;
            }
            finally
            {
                ClearProgressBar();
            }
        }

        private static void OnCopyFile(string sourcePath, string destPath)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
                File.SetAttributes(destPath, File.GetAttributes(sourcePath));
            }

            var files = Directory.GetFiles(sourcePath);
            for (int i = 0; i < files.Length; i++)
            {
                File.Copy(files[i], $"{destPath}/{Path.GetFileName(files[i])}", true);
                ProgressBar("Copy AssetBundle", $"{destPath}\n\r{files[i]}", ((float)i / (float)files.Length));
            }

            var dirs = Directory.GetDirectories(sourcePath);
            foreach (string dir in dirs)
            {
                OnCopyFile(dir, $"{destPath}/{Path.GetFileName(dir)}");
            }
        }

#endif
    }
}