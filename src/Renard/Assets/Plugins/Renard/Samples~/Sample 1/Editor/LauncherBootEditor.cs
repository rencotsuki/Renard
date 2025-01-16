using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Renard.Sample
{
    public class LauncherBootEditor : IPreprocessBuildWithReport
    {
        private const string BootScene = "Launcher";

        private static int _activeBoot = -1;
        private const string _launcherBoot = "LauncherBoot";

        protected static bool IsStarter
        {
            get
            {
                if (_activeBoot == -1)
                    _activeBoot = EditorPrefs.GetBool(_launcherBoot, true) ? 1 : 0;
                return _activeBoot != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != _activeBoot)
                {
                    _activeBoot = newValue;
                    EditorPrefs.SetBool(_launcherBoot, value);
                }
            }
        }

        public int callbackOrder { get { return 0; } }

        // ビルド前にアセットを更新させるための処理
        public void OnPreprocessBuild(BuildReport report)
        {
            AssetDatabase.Refresh();
        }

        // エディタ再生前にアセットを更新させるための処理
        [InitializeOnLoadMethod]
        public static void Run()
        {
            EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    AssetDatabase.Refresh();
                }
            };
        }

        [RuntimeInitializeOnLoadMethod]
        public static void Starter()
        {
            if (!IsStarter) return;

            ClearConsole();

            SceneManager.LoadScene(BootScene, LoadSceneMode.Single);
        }

        private static void ClearConsole()
        {
            var type = Assembly.GetAssembly(typeof(SceneView))
#if UNITY_2017_1_OR_NEWER
                .GetType("UnityEditor.LogEntries");
#else
                .GetType( "UnityEditorInternal.LogEntries" );
#endif
            var method = type.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            method.Invoke(null, null);

            //Debug.Log("LauncherBootEditor::ClearConsole - Clear console log.");
        }

        [MenuItem("Renard/Boot", false, 1)]
        public static void ToggleBootMode()
        {
            IsStarter = !IsStarter;
        }

        [MenuItem("Renard/Boot", true, 1)]
        public static bool ToggleBootModeValidate()
        {
            Menu.SetChecked("Renard/Boot", IsStarter);
            return true;
        }
    }
}