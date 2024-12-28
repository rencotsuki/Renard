using System;
using UnityEngine;

namespace Renard
{
    /// <summary>※Renard拡張機能</summary>
    public class ApplicationCopyright
    {
        /// <summary>初年度</summary>
        public static string FirstYear = "2024";

        /// <summary>今年度</summary>
        public static string NowYear => $"{DateTime.Now.Year}";

        /// <summary>コピーライト表記: © [companyName]</summary>
        public static string Copyright1 => $"© {Application.companyName}.";

        /// <summary>コピーライト表記：© [FirstYear] [companyName]</summary>
        public static string Copyright2 => $"© {FirstYear} {Application.companyName}.";

        /// <summary>コピーライト表記：© [FirstYear]-[NowYear] [companyName]</summary>
        public static string Copyright3 => $"© {FirstYear}-{NowYear} {Application.companyName}.";
    }
}
