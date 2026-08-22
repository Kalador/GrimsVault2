using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using Vault.Core.Internal;

namespace Vault.Core
{
    /// <summary>
    /// Minimal contract for a ViewMode.
    /// </summary>
    public abstract class VaultViewMode(string name)
    {
        /// <summary>
        /// Unique identifier for this ViewMode.
        /// Used for registration, theme switching, and discovery.
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// Absolute virtual path to the main .cshtml file.
        /// </summary>
        public abstract string ViewPath { get; }

        /// <summary>
        /// Absolute virtual path to the card partial.
        /// Required so retry actions can render cards consistently.
        /// </summary>
        public abstract string CardPartialPath { get; }

        /// <summary>
        /// Optional CSS files.
        /// </summary>
        public virtual IReadOnlyList<string> Styles => [];

        /// <summary>
        /// Optional JS files.
        /// </summary>
        public virtual IReadOnlyList<string> Scripts => [];

        public static string CacheBust => Versioning.CacheBust;

        /// <summary>
        /// Class used to resolve caching issues with CSS. Allows every build to force most recent style changes
        /// </summary>
        private static class Versioning
        {
            public static readonly string CacheBust;

            static Versioning()
            {
                var location = Assembly.GetExecutingAssembly().Location;

                if (string.IsNullOrWhiteSpace(location))
                {
                    CacheBust = DateTime.UtcNow.Ticks.ToString();
                    return;
                }

                var info = FileVersionInfo.GetVersionInfo(location);
                CacheBust = info.FileVersion ?? DateTime.UtcNow.Ticks.ToString();
            }
        }


    }

    public static class VaultViewModes
    {
        private static readonly List<VaultViewMode> _all = [];
        public static IReadOnlyList<VaultViewMode> All => _all;

        internal static void Register(VaultViewMode mode) => _all.Add(mode);
    }

    public static class VaultViewModeExtensions
    {
        public static T Register<T>(this T mode) where T : VaultViewMode
        {
            VaultViewModes.Register(mode);
            return mode;
        }
    }



    


}
