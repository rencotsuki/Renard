﻿using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renard.AssetBundleUniTask
{
    public class MonoInstallationFinder
    {
        public static string GetFrameWorksFolder()
        {
#if UNITY_EDITOR
            var editorAppPath = EditorApplication.applicationPath;
#else
            var editorAppPath = $"{Application.dataPath}/.." ;
#endif

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return Path.Combine(Path.GetDirectoryName(editorAppPath), "Data");
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                return Path.Combine(editorAppPath, Path.Combine("Contents", "Frameworks"));
            }
            else // Linux...?
            {
                return Path.Combine(Path.GetDirectoryName(editorAppPath), "Data");
            }
        }

#if UNITY_EDITOR
        public static string GetProfileDirectory(BuildTarget target, string profile)
        {
            var monoprefix = GetMonoInstallation();
            return Path.Combine(monoprefix, Path.Combine("lib", Path.Combine("mono", profile)));
        }
#endif

        public static string GetMonoInstallation()
        {
#if INCLUDE_MONO_2_12
            return GetMonoInstallation("MonoBleedingEdge");
#else
            return GetMonoInstallation("Mono");
#endif
        }

        public static string GetMonoInstallation(string monoName)
        {
            return Path.Combine(GetFrameWorksFolder(), monoName);
        }
    }

    public class ExecuteInternalMono
    {
        private static readonly Regex _unsafeCharsWindows = new Regex("[^A-Za-z0-9\\_\\-\\.\\:\\,\\/\\@\\\\]");
        private static readonly Regex _unescapeableChars = new Regex("[\\x00-\\x08\\x10-\\x1a\\x1c-\\x1f\\x7f\\xff]");
        private static readonly Regex _quotes = new Regex("\"");

        public ProcessStartInfo ProcessStartInfo = null;

        public static string PrepareFileName(string input)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                return EscapeCharsQuote(input);
            }
            return EscapeCharsWindows(input);
        }

        public static string EscapeCharsQuote(string input)
        {
            if (input.IndexOf('\'') == -1)
            {
                return "'" + input + "'";
            }
            if (input.IndexOf('"') == -1)
            {
                return "\"" + input + "\"";
            }
            return null;
        }

        public static string EscapeCharsWindows(string input)
        {
            if (input.Length == 0)
            {
                return "\"\"";
            }
            if (_unescapeableChars.IsMatch(input))
            {
                UnityEngine.Debug.LogWarning("Cannot escape control characters in string");
                return "\"\"";
            }
            if (_unsafeCharsWindows.IsMatch(input))
            {
                return "\"" + _quotes.Replace(input, "\"\"") + "\"";
            }
            return input;
        }

        public static ProcessStartInfo GetProfileStartInfoForMono(string monodistribution, string profile, string executable, string arguments, bool setMonoEnvironmentVariables)
        {
            var monoexe = PathCombine(monodistribution, "bin", "mono");
            var profileAbspath = PathCombine(monodistribution, "lib", "mono", profile);

            if (Application.platform == RuntimePlatform.WindowsEditor)
                monoexe = PrepareFileName(monoexe + ".exe");

            var startInfo = new ProcessStartInfo
            {
                Arguments = PrepareFileName(executable) + " " + arguments,
                CreateNoWindow = true,
                FileName = monoexe,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = Application.dataPath + "/..",
                UseShellExecute = false
            };

            if (setMonoEnvironmentVariables)
            {
                startInfo.EnvironmentVariables["MONO_PATH"] = profileAbspath;
                startInfo.EnvironmentVariables["MONO_CFG_DIR"] = PathCombine(monodistribution, "etc");
            }
            return startInfo;
        }

        private static string PathCombine(params string[] parts)
        {
            var path = parts[0];
            for (var i = 1; i < parts.Length; ++i)
            {
                path = Path.Combine(path, parts[i]);
            }
            return path;
        }
    }
}
