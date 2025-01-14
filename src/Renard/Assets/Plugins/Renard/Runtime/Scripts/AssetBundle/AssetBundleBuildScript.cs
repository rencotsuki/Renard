using System.IO;
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

        public static void BuildAssetBundles(string outputDirectory, bool isEncrypt, bool isViewStatusBar = false)
            => BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget, outputDirectory, isEncrypt, isViewStatusBar);

        public static void BuildAssetBundles(BuildTarget buildTarget, string outputDirectory, bool isEncrypt, bool isViewStatusBar = false)
        {
            var outputPath = $"{outputDirectory}/{AssetBundleBuildConfig.FolderName}/{AssetBundleBuildConfig.GetPlatformDirectoryName(buildTarget)}";
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            Debug.Log($"BuildAssetBundles:[{buildTarget}] {outputPath}");

            var manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, buildTarget);

            CreateAssetBundleHashList(outputPath, buildTarget, manifest, isViewStatusBar);

            if (isEncrypt)
                AssetBundleEncryptionAndCreateHashList(outputPath, buildTarget, manifest, isEncrypt, isViewStatusBar);

            if (isViewStatusBar) EditorUtility.ClearProgressBar();

            Debug.Log($"BuildAssetBundles:[{buildTarget}] <color=yellow>Completed !</color>");
        }

        private static AssetBundleHashList OnLoadAssetBundleHashList(string path, string fileName)
        {
            try
            {
                var result = new AssetBundleHashList();
                if (File.Exists($"{path}/{fileName}.{AssetBundleConfig.HashFileExtension}"))
                {
                    // TODO: UTF8のBomに注意！！
                    var data = File.ReadAllText($"{path}/{fileName}.{AssetBundleConfig.HashFileExtension}", new System.Text.UTF8Encoding(false));

                    if (data != null && data.Length > 0)
                        result.Deserialize(data);
                }

                Debug.Log($"OnLoadAssetBundleHashList: <color=yellow>Success</color>.\r\npath={path}/{fileName}.{AssetBundleConfig.HashFileExtension}");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"OnLoadAssetBundleHashList: <color=red>Failed</color>. {ex.Message}");
                return null;
            }
        }

        private static bool OnSaveAssetBundleHashList(AssetBundleHashList data, string outputPath, string fileName)
        {
            try
            {
                if (data == null)
                    throw new System.Exception($"AssetBundleHashList null. path={outputPath}/{fileName}");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                // TODO: UTF8のBomに注意！！
                File.WriteAllText($"{outputPath}/{fileName}", data.Serialize(), new System.Text.UTF8Encoding(false));
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"OnSaveAssetBundleHashList: <color=red>Failed</color>. {ex.Message}");
                return false;
            }
        }

        private static void CreateAssetBundleHashList(string basePath, BuildTarget buildTarget, AssetBundleManifest manifest, bool isViewStatusBar)
        {
            if (manifest == null)
            {
                Debug.LogWarning("CreateAssetBundleHashList: No assetBundle output manifest.");
                return;
            }

            var assetBundleHashList = OnLoadAssetBundleHashList(basePath, AssetBundleConfig.HashFileName);
            var assetBundles = manifest.GetAllAssetBundles();

            var manifestName = AssetBundleBuildConfig.GetPlatformManifestName(buildTarget);
            if (isViewStatusBar) EditorUtility.DisplayProgressBar("AssetBundleEncryption", $"{manifestName}", 0);

            // AssetBundle Manifest
            {
                var dataSize = File.ReadAllBytes(Path.Combine(basePath, manifestName)).Length;
                if (dataSize <= 0)
                {
                    Debug.LogWarning($"CreateAssetBundleHashList: <color=red>Failed</color>. {manifestName}");
                    return;
                }

                var manifestDataSize = File.ReadAllBytes(Path.Combine(basePath, $"{manifestName}.manifest")).Length; 
                if (manifestDataSize <= 0)
                {
                    Debug.LogWarning($"CreateAssetBundleHashList: <color=red>Failed</color>. {manifestName}.manifest");
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
                    Debug.LogWarning($"CreateAssetBundleHashList: <color=red>Failed</color>. {assetBundles[i]}");
                }

                var manifestDataSize = File.ReadAllBytes(Path.Combine(basePath, $"{assetBundles[i]}.manifest")).Length;
                if (manifestDataSize <= 0)
                {
                    Debug.LogWarning($"CreateAssetBundleHashList: <color=red>Failed</color>. {assetBundles[i]}.manifest");
                }

                uint crc = 0;
                BuildPipeline.GetCRCForAssetBundle(Path.Combine(basePath, assetBundles[i]), out crc);

                var data = AssetBundleInfo.Create(assetBundles[i], hash.ToString(), dataSize, manifestDataSize, crc);
                assetBundleHashList.Assets.Add(assetBundles[i], data);
            }

            assetBundleHashList.CreateTime = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            OnSaveAssetBundleHashList(assetBundleHashList, basePath, $"{AssetBundleConfig.HashFileName}.{AssetBundleConfig.HashFileExtension}");

            AssetDatabase.Refresh();

            Debug.Log($"CreateAssetBundleHashList:[{buildTarget}]\n{assetBundleHashList.ToString()}");
        }

        private static void AssetBundleEncryptionAndCreateHashList(string basePath, BuildTarget buildTarget, AssetBundleManifest manifest, bool isEncrypt, bool isViewStatusBar)
        {
            if (manifest == null)
            {
                Debug.LogWarning("AssetBundleEncryptionAndCreateHashList: No assetBundle output manifest.");
                return;
            }

            var outputPath = $"{basePath}/Encrypt";
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);

            Directory.CreateDirectory(outputPath);

            var assetBundleHashList = OnLoadAssetBundleHashList(outputPath, AssetBundleConfig.HashFileName);
            var assetBundles = manifest.GetAllAssetBundles();

            var manifestName = AssetBundleBuildConfig.GetPlatformManifestName(buildTarget);
            if (isViewStatusBar) EditorUtility.DisplayProgressBar("AssetBundleEncryption", $"{manifestName}", 0);

            // AssetBundle Manifest
            {
                var dataSize = EncryptionAssetBundle(manifestName, Path.Combine(basePath, manifestName), Path.Combine(outputPath, manifestName));
                if (dataSize <= 0)
                {
                    Debug.LogWarning($"AssetBundleEncryptionAndCreateHashList: EncryptionAssetBundle <color=red>Failed</color>. {manifestName}");
                    return;
                }

                var manifestDataSize = EncryptionAssetBundle(manifestName, Path.Combine(basePath, $"{manifestName}.manifest"), Path.Combine(outputPath, $"{manifestName}.manifest"));
                if (manifestDataSize <= 0)
                {
                    Debug.LogWarning($"AssetBundleEncryptionAndCreateHashList: EncryptionAssetBundle <color=red>Failed</color>. {manifestName}.manifest");
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

                var dataSize = EncryptionAssetBundle(assetBundles[i], Path.Combine(basePath, assetBundles[i]), Path.Combine(outputPath, assetBundles[i]));
                if (dataSize <= 0)
                {
                    Debug.LogWarning($"AssetBundleEncryptionAndCreateHashList: EncryptionAssetBundle <color=red>Failed</color>. {assetBundles[i]}");
                }

                var manifestDataSize = EncryptionAssetBundle($"{assetBundles[i]}.manifest", Path.Combine(basePath, $"{assetBundles[i]}.manifest"), Path.Combine(outputPath, $"{assetBundles[i]}.manifest"));
                if (manifestDataSize <= 0)
                {
                    Debug.LogWarning($"AssetBundleEncryptionAndCreateHashList: EncryptionAssetBundle <color=red>Failed</color>. {assetBundles[i]}.manifest");
                }

                uint crc = 0;
                BuildPipeline.GetCRCForAssetBundle(Path.Combine(basePath, assetBundles[i]), out crc);

                var data = AssetBundleInfo.Create(assetBundles[i], hash.ToString(), dataSize, manifestDataSize, crc);
                assetBundleHashList.Assets.Add(assetBundles[i], data);
            }

            assetBundleHashList.CreateTime = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            OnSaveAssetBundleHashList(assetBundleHashList, outputPath, $"{AssetBundleConfig.HashFileName}.{AssetBundleConfig.HashFileExtension}");

            AssetDatabase.Refresh();

            Debug.Log($"AssetBundleEncryptionAndCreateHashList:[{buildTarget}]\n{assetBundleHashList.ToString()}");
        }

        private static int EncryptionAssetBundle(string assetName, string basePath, string outputPath)
        {
            if (string.IsNullOrEmpty(assetName)) return 0;

            if (File.Exists(basePath))
            {
                var data = File.ReadAllBytes(basePath);
                if (data.Length > 0)
                {
                    // 暗号化してファイルに書き込む
                    using (var baseStream = new FileStream(outputPath, FileMode.OpenOrCreate))
                    {
                        var uniqueSalt = System.Text.Encoding.UTF8.GetBytes(assetName);
                        var cryptor = new SeekableAesStream(baseStream, AssetBundleBuildConfig.Encrypt_KEY, uniqueSalt);
                        cryptor.Write(data, 0, data.Length);
                        Debug.Log($"EncryptionFile:[<color=yellow>success</color>] {basePath} => {outputPath}");
                        return data.Length;
                    }
                }
            }

            Debug.Log($"EncryptionFile:[<color=red>failed</color>] {basePath} => {outputPath}");
            return 0;
        }

#endif
    }
}