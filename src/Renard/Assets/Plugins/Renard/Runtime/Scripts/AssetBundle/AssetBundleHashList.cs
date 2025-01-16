using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Renard.AssetBundleUniTask
{
    using Debuger;

    [Serializable]
    public struct AssetBundleInfo
    {
        public string AssetName;

        public int AssetSize;

        public string AssetHash;

        public uint AssetCRC;

        public string ManifestName;

        public int ManifestSize;

        public static AssetBundleInfo Create(string assetName)
        {
            return Create(assetName, string.Empty, 0, 0);
        }
        public static AssetBundleInfo Create(string assetName, string hash, int assetSize, int manifestName, uint crc = 0)
        {
            var info = new AssetBundleInfo();
            info.SetAssetInfo(assetName, hash, assetSize, crc);
            info.SetManifestInfo(assetName, manifestName);
            return info;
        }

        public void SetAssetInfo(string name, string hash, int size, uint crc = 0)
        {
            AssetName = name;
            AssetHash = hash;
            AssetSize = size;
            AssetCRC = crc;
        }

        public void SetManifestInfo(string name, int size)
        {
            ManifestName = $"{name}.manifest";
            ManifestSize = size;
        }
    }

    [Serializable]
    public class AssetBundleHashList
    {
        public string CreateTime = string.Empty;
        public AssetBundleInfo PlatformMaster = new AssetBundleInfo();

        [SerializeField] private List<string> _keys = new List<string>();
        [SerializeField] private List<AssetBundleInfo> _values = new List<AssetBundleInfo>();

        public Dictionary<string, AssetBundleInfo> Assets = new Dictionary<string, AssetBundleInfo>();

        public int Count { get { return Assets != null ? Assets.Count : 0; } }

        protected void Log(DebugerLogType logType, string methodName, string message)
            => DebugLogger.Log(this.GetType(), logType, methodName, message);

        public string Serialize()
        {
            try
            {
                _keys = Assets.Keys.ToList();
                _values = Assets.Values.ToList();

                return JsonUtility.ToJson(this, true);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "Serialize", $"{ex.Message}");
                return string.Empty;
            }
        }

        public byte[] ToByte()
        {
            try
            {
                return Encoding.UTF8.GetBytes(Serialize());
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "ToByte", $"{ex.Message}");
                return new byte[0];
            }
        }

        public void Deserialize(byte[] data)
            => Deserialize(data != null && data.Length > 0 ? Encoding.UTF8.GetString(data) : string.Empty);
        public void Deserialize(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                    throw new Exception("DataJson null or empty.");

                Copy(JsonUtility.FromJson<AssetBundleHashList>(json));
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "Deserialize", $"{ex.Message}");
            }
        }

        public void Copy(AssetBundleHashList data)
        {
            if (data == null) return;

            CreateTime = data.CreateTime;
            PlatformMaster = data.PlatformMaster;

            _keys = data._keys;
            _values = data._values;

            if (Assets == null)
                Assets = new Dictionary<string, AssetBundleInfo>();

            Assets?.Clear();

            for (int i = 0; i < _keys.Count; i++)
            {
                if (_values.Count > i)
                    Assets.Add(_keys[i], _values[i]);
            }
        }

        public AssetBundleInfo GetAsset(string assetName)
        {
            if (Assets.ContainsKey(assetName)) return Assets[assetName];
            return new AssetBundleInfo();
        }
    }
}
