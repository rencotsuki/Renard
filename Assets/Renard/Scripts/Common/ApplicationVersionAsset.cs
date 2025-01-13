using System;
using UnityEngine;

namespace Renard
{
    [Serializable]
    public class ApplicationVersionAsset : ScriptableObject
    {
        public const string Path = "Assets/Renard/Resources";
        public const string FileName = "ApplicationVersion";
        public const string FileExtension = "asset";

        [SerializeField] private string _version = "0.0.0";
        public string Version => _version;

        [SerializeField] private int _buildNumber = 0;
        public int BuildNumber => _buildNumber;

        public static ApplicationVersionAsset Load()
        {
            try
            {
                return Resources.Load<ApplicationVersionAsset>(FileName);
            }
            catch (Exception ex)
            {
                Debug.Log($"ApplicationVersionData::Load <color=red>error</color>. {ex.Message}");
                return null;
            }
        }

        public void Set(string version, int buildNumber)
        {
            _version = version;
            _buildNumber = buildNumber;
        }
    }
}
