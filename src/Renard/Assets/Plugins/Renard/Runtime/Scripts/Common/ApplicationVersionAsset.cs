using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace Renard
{
    using Debuger;

    [Serializable]
    public class ApplicationVersionAsset : ScriptableObject
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "ApplicationVersion";
        public const string FileExtension = "asset";

        [Header("※Standalone用：必ずResources下に置いてください")]

        [Header("バージョン")]
        [SerializeField, HideInInspector] private string _version = "0.0.0";
        public string Version => _version;

        [Header("ビルド")]
        [SerializeField, HideInInspector] private int _buildNumber = 0;
        public int BuildNumber => _buildNumber;

        [Header("コミットハッシュ")]
        [SerializeField, HideInInspector] private string _commitHash = string.Empty;
        public string CommitHash => _commitHash;
        public string CommitHashShort => string.IsNullOrEmpty(_commitHash) ? string.Empty : _commitHash.Substring(0, (_commitHash.Length > 7 ? 7 : _commitHash.Length));

        public static ApplicationVersionAsset Load()
        {
            try
            {
                return Resources.Load<ApplicationVersionAsset>(FileName);
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(ApplicationVersionAsset).Name}::Load <color=red>error</color>. {ex.Message}");
                return null;
            }
        }

        public void Update()
        {
#if UNITY_EDITOR
            _version = Application.version;
            _buildNumber = int.Parse(PlayerSettings.macOS.buildNumber);
            _commitHash = DebugDotGit.GetCurrentCommitHash();
#endif
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(ApplicationVersionAsset))]
    public class ApplicationVersionAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var handler = target as ApplicationVersionAsset;

            EditorGUILayout.Space(10);

            var helpMessage = "あらかじめ作成しておくかStandaloneのビルド時に自動生成されます。\n\r\n\r";
            helpMessage += "値に関しては、\n\r";
            helpMessage += "Version ⇒ Application.version\n\r";
            helpMessage += "BuildNumber ⇒ PlayerSettings.macOS.buildNumber\n\r";
            helpMessage += "CommitHash ⇒ 「.git/HEAD」 -> ref: Head commitHash\n\r";
            helpMessage += "を参照して自動更新されます。";

            EditorGUILayout.HelpBox(helpMessage, MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Version", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{handler.Version}");

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("BuildNumber", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{handler.BuildNumber}");

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("CommitHash", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{(string.IsNullOrEmpty(handler.CommitHashShort) ? "---" : handler.CommitHashShort)}");

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"{(string.IsNullOrEmpty(handler.CommitHash) ? "---" : handler.CommitHash)}");

            EditorGUILayout.Space(25);

            if (GUILayout.Button("Update"))
            {
                handler.Update();
                EditorUtility.SetDirty(handler);
            }
        }
    }

    public class ApplicationVersionEditor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            // ビルド時に情報を更新する
            CreateApplicationVersion();
        }

        [MenuItem("Assets/Create/Renard/ApplicationVersion")]
        private static void CreateApplicationVersion()
        {
            try
            {
                // １回ロードしてAssetが存在するか確認する
                var asset = ApplicationVersionAsset.Load();
                if (asset != null)
                {
                    asset.Update();

                    EditorUtility.SetDirty(asset);
                }
                else
                {
                    if (!Directory.Exists(ApplicationVersionAsset.Path))
                        Directory.CreateDirectory(ApplicationVersionAsset.Path);

                    asset = new ApplicationVersionAsset();

                    asset.Update();

                    EditorUtility.SetDirty(asset);
                    AssetDatabase.CreateAsset(asset, $"{ApplicationVersionAsset.Path}/{ApplicationVersionAsset.FileName}.{ApplicationVersionAsset.FileExtension}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(ApplicationVersionEditor)}::CreateApplicationVersion <color=red>Failed</color>. {ex.Message}");
            }
        }
    }

#endif
}
