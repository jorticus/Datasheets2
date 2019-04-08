using DotNet.Globbing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datasheets2
{
    public static class Settings
    {
        /// <summary>
        /// The documents root to display.
        /// Can be set through Datasheets.exe.config.
        /// Defaults to the current directory.
        /// </summary>
        public static string DocumentsDir => _documentsDir.Value;
        private static Lazy<string> _documentsDir = new Lazy<string>(() =>
        {
            var dir = ConfigurationManager.AppSettings.Get("DocumentsDir");
            if (String.IsNullOrWhiteSpace(dir) || !System.IO.Directory.Exists(dir))
                dir = System.IO.Directory.GetCurrentDirectory();
            return dir;
        });

        /// <summary>
        /// Whether online search should be allowed
        /// </summary>
        public static bool AllowOnlineSearch => 
            ParseBool(ConfigurationManager.AppSettings.Get("AllowOnlineSearch"), defaultValue: true);

        /// <summary>
        /// Whether the file extension should be shown
        /// </summary>
        public static bool ShowExtension => _showExtension.Value;
        private static Lazy<bool> _showExtension = new Lazy<bool>(
            () => ParseBool(ConfigurationManager.AppSettings.Get("ShowExtension"), defaultValue: false));

        /// <summary>
        /// File types that can be previewed from the internet.
        /// IMPORTANT: Do not allow any executable types, as files from the internet are untrusted and could be malicious.
        /// </summary>
        public static List<string> AllowedPreviewTypes => _allowedPreviewTypes.Value;
        private static Lazy<List<string>> _allowedPreviewTypes = new Lazy<List<string>>(
            () => ParseList(ConfigurationManager.AppSettings.Get("AllowedPreviewTypes"), ';') ?? new List<string>());

        /// <summary>
        /// If set, only show files matching these extensions
        /// </summary>
        public static List<string> IncludeFilter => _includeFilter.Value;
        private static Lazy<List<string>> _includeFilter = new Lazy<List<string>>(
            () => ParseList(ConfigurationManager.AppSettings.Get("IncludeFilter"), ';'));

        /// <summary>
        /// If set, don't show files matching these extensions
        /// </summary>
        public static List<string> ExcludeFilter => _excludeFilter.Value;
        private static Lazy<List<string>> _excludeFilter = new Lazy<List<string>>(
            () => ParseList(ConfigurationManager.AppSettings.Get("ExcludeFilter"), ';'));

        public static List<Glob> IncludeGlob => _includeGlob.Value;
        private static Lazy<List<Glob>> _includeGlob = new Lazy<List<Glob>>(() =>
            IncludeFilter?.Select(filter => Glob.Parse(filter))?.ToList());

        public static List<Glob> ExcludeGlob => _excludeGlob.Value;
        private static Lazy<List<Glob>> _excludeGlob = new Lazy<List<Glob>>(() =>
            ExcludeFilter?.Select(filter => Glob.Parse(filter))?.ToList());

        #region Helpers

        private static bool ParseBool(string s, bool defaultValue)
        {
            s = s?.ToLowerInvariant();
            if (s == "false")
                return false;
            if (s == "true")
                return true;

            return defaultValue;
            //throw new InvalidCastException($"Value is not a valid boolean (was '{s}')");
        }

        private static List<string> ParseList(string s, params char[] separator)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            return s
                .Split(separator)
                .Select(token => token.Trim())
                .Where(token => !String.IsNullOrEmpty(token))
                .ToList();
        }

        #endregion
    }
}
