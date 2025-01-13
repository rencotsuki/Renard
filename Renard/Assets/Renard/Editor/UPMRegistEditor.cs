using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Renard
{
    /*
     * UnityPackageManager(UPM)パッケージを自動的インポートするためのスクリプト
     */

    public class UPMRegistEditor
    {
        private static AddRequest addRequest;

        // インポートしたいパッケージ
        private static readonly string[] importPackges = new string[]
            {
                "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
                "https://github.com/neuecc/UniRx.git?path=Assets/Plugins/UniRx/Scripts",
            };

        [InitializeOnLoadMethod]
        [MenuItem("Renard/Install Package", false)]
        public static void InstallPackage()
        {
            if (importPackges == null || importPackges.Length <= 0)
                return;

            foreach (var package in importPackges)
            {
                addRequest = Client.Add(package);
            }

            EditorApplication.update += Progress;
        }

        private static void Progress()
        {
            if (addRequest.IsCompleted)
            {
                if (addRequest.Status == StatusCode.Success)
                {
                    Debug.Log($"{typeof(UPMRegistEditor).Name}::Progress - installed package. {addRequest.Result.packageId}");
                }
                else if (addRequest.Status >= StatusCode.Failure)
                {
                    Debug.LogError($"{typeof(UPMRegistEditor).Name}::Progress - failed to install package. {addRequest.Error.message}");
                }

                EditorApplication.update -= Progress;
            }
        }
    }
}