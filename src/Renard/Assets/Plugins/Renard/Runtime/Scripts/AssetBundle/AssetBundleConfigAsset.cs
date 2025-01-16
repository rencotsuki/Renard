using System;
using UnityEngine;

namespace Renard.AssetBundleUniTask
{
    [CreateAssetMenu(menuName = "Renard/AssetBundleConfig", fileName = FileName)]
    public class AssetBundleConfigAsset : ScriptableObject
    {
        public const string FileName = "AssetBundleConfig";
        public const string FileExtension = "asset";

        [Header("※必ずResources下に置いてください")]

        [Header("暗号化の有効")]
        [SerializeField] private bool _isEncrypt = false;
        public bool IsEncrypt => _isEncrypt;

        [Header("暗号化キー情報")]
        [SerializeField] private string _encryptKey = "RenardAsset";
        public string EncryptIV => $"iv_{_encryptKey}";
        public string EncryptKey => $"key_{_encryptKey}";

        [Header("ローカル内パス(空でも可)")]
        [SerializeField] private string _outputPath = AssetBundleManager.DefaultOutputPath;
        public string OutputPath => _outputPath;

        public static AssetBundleConfigAsset Load()
        {
            try
            {
                return Resources.Load<AssetBundleConfigAsset>(FileName);
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(AssetBundleConfigAsset).Name}::Load <color=red>error</color>. {ex.Message}");
                return null;
            }
        }
    }
}
