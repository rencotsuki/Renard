using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Renard
{
    /*
     * UnityPackageManager(UPM)パッケージを自動的インポートするためのスクリプト
     */

    //[InitializeOnLoad]
    public static class UPMRegistEditor
    {
        // インポートしたいパッケージ
        private static readonly string[] importPackges = new string[]
            {
                "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
                "https://github.com/neuecc/UniRx.git?path=Assets/Plugins/UniRx/Scripts",
            };

        private static AddRequest addRequest = null;
        private static List<string> targets = null;

        static UPMRegistEditor()
        {
            EditorApplication.update += RunOnceOnStartup;
        }

        private static void RunOnceOnStartup()
        {
            InstallPackages();
            EditorApplication.update -= RunOnceOnStartup;
        }

        [MenuItem("Renard/Install Packages", false)]
        public static void InstallPackages()
        {
            //TODO: もう少し上手い処理を考えた方が良さそう
            InstallPackage();
        }

        private static void InstallPackage()
        {
            try
            {
                if (targets == null || targets.Count <= 0)
                    return;

                foreach (var package in targets)
                {
                    addRequest = Client.Add(package);
                }

                EditorApplication.update += ProgressInstall;
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(UPMRegistEditor).Name}::InstallPackage - {ex.Message}");
            }
        }

        private static void ProgressInstall()
        {
            try
            {
                if (addRequest.IsCompleted)
                {
                    if (addRequest.Status == StatusCode.Success)
                    {
                        Debug.Log($"{typeof(UPMRegistEditor).Name}::ProgressInstall - installed package. {addRequest.Result.packageId}");
                    }
                    else if (addRequest.Status >= StatusCode.Failure)
                    {
                        Debug.LogError($"{typeof(UPMRegistEditor).Name}::ProgressInstall - failed to install package. {addRequest.Error.message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(UPMRegistEditor).Name}::ProgressInstall - {ex.Message}");
            }
            finally
            {
                EditorApplication.update -= ProgressInstall;
            }
        }
    }
}