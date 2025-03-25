using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Renard.Debuger
{
    [Serializable]
    public static class DebugDotGit
    {
        private const string dotGitFolder = ".git";
        private const string headFile = "HEAD";
        private const string modulesFile = ".git";
        private const int defaultSearchStepUp = 3;

        private static string searchBeginPath => $"{Application.dataPath}/..";
        private static Encoding fileEncoding => Encoding.UTF8;

        public static string GetCurrentCommitHash(int searchStepUp = defaultSearchStepUp)
        {
            try
            {
                var folderPath = SearchFolder(dotGitFolder, searchBeginPath, searchStepUp);
                var data = GetHeadRefPath(folderPath, headFile);
                var headRefPath = data.Replace("ref: ", "").TrimStart().TrimEnd();
                if (!string.IsNullOrEmpty(headRefPath))
                {
                    return GetHeadHash($"{folderPath}/{headRefPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(DebugDotGit)}::GetCurrentCommitHash <color=red>Failed</color>. {ex.Message}");
            }
            return string.Empty;
        }

        public static string GetSubmodulesCommitHash(string modulesPath, int searchStepUp = defaultSearchStepUp)
        {
            try
            {
                var gitModulesPath = SearchModulesFile(modulesPath, searchStepUp);
                var data = GetHeadRefPath(gitModulesPath, headFile);
                var headRefPath = data.Replace("ref: ", "").TrimStart().TrimEnd();
                if (!string.IsNullOrEmpty(headRefPath))
                {
                    return GetHeadHash($"{gitModulesPath}/{headRefPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(DebugDotGit)}::GetSubmodulesCommitHash <color=red>Failed</color>. {ex.Message}");
            }
            return string.Empty;
        }

        private static string SearchFolder(string folderName, string beginPath, int stepUp)
        {
            try
            {
                if (stepUp > 0)
                {
                    var resultPath = beginPath;
                    for (int i = 0; i <= stepUp; i++)
                    {
                        if (Directory.Exists($"{resultPath}/{folderName}"))
                            return $"{resultPath}/{folderName}";

                        resultPath += "/..";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(DebugDotGit)}::SearchFolder <color=red>Failed</color>. {ex.Message}");
            }
            return string.Empty;
        }

        private static string SearchModulesFile(string beginPath, int stepUp)
        {
            try
            {
                if (stepUp > 0)
                {
                    var resultPath = beginPath;
                    for (int i = 0; i <= stepUp; i++)
                    {
                        if (File.Exists($"{resultPath}/{modulesFile}"))
                        {
                            var data = GetHeadRefPath(resultPath, modulesFile);
                            var gitDirPath = data.Replace("gitdir: ", "").TrimStart().TrimEnd();
                            if (!string.IsNullOrEmpty(gitDirPath))
                                return $"{resultPath}/{gitDirPath}";

                            break;
                        }

                        resultPath += "/..";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(DebugDotGit)}::SearchFolder <color=red>Failed</color>. {ex.Message}");
            }
            return string.Empty;
        }

        private static string ReadTextFile(string filePath, Encoding encoding)
        {
            try
            {
                // ファイルが無い場合は無視
                if (File.Exists(filePath))
                {
                    var readData = File.ReadAllText(filePath, encoding);
                    readData = readData.Replace("\n\r", "").Trim();
                    return readData;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{typeof(DebugDotGit)}::ReadTextFile <color=red>Failed</color>. {ex.Message}");
            }
            return string.Empty;
        }

        private static string GetHeadRefPath(string path, string fileName)
            => ReadTextFile($"{path}/{fileName}", fileEncoding);

        private static string GetHeadHash(string filePath)
            => ReadTextFile(filePath, fileEncoding);
    }
}
