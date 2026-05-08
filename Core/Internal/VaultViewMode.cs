using System.Reflection;

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

        public static string CacheBust => Assembly.GetExecutingAssembly().GetName().Version?.ToString()  ?? DateTime.UtcNow.Ticks.ToString();
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
