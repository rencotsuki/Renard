using System;
using UnityEngine;

namespace Renard
{
    public static class ApplicationVersionUtil
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "ApplicationVersion";
        public const string FileExtension = "asset";
    }

    [Serializable]
    public class ApplicationVersionAsset : ScriptableObject
    {
        [SerializeField] private string _version = "0.0.0";
        public string Version => _version;

        [SerializeField] private int _buildNumber = 0;
        public int BuildNumber => _buildNumber;

        public static ApplicationVersionAsset Load()
        {
            try
            {
                return Resources.Load<ApplicationVersionAsset>(ApplicationVersionUtil.FileName);
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(ApplicationVersionAsset).Name}::Load <color=red>error</color>. {ex.Message}");
                return null;
            }
        }

        public void Set(string version, int buildNumber)
        {
            _version = version;
            _buildNumber = buildNumber;
        }

        public void Save()
        {
#if UNITY_EDITOR
            var fullPath = $"{ApplicationVersionUtil.Path}/{ApplicationVersionUtil.FileName}.{ApplicationVersionUtil.FileExtension}";

            try
            {
                if (!System.IO.Directory.Exists(ApplicationVersionUtil.Path))
                    System.IO.Directory.CreateDirectory(ApplicationVersionUtil.Path);

                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.CreateAsset(this, fullPath);

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.Log($"{this.GetType().Name}::Save <color=red>error</color>. {ex.Message}\r\npath={fullPath}");
            }
#endif
        }
    }
}
