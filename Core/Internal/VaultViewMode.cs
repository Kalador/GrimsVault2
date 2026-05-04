namespace Vault.Core
{
    /// <summary>
    /// Base type for all Vault ViewModes.
    ///
    /// Each ViewMode is a drop-in module located under:
    ///     /ViewModes/{Name}/
    ///
    /// A ViewMode defines:
    /// - Its name
    /// - Its view path
    /// - Its own CSS/JS assets (optional)
    ///
    /// Assets are loaded dynamically by the client when the ViewMode is activated.
    /// </summary>
    public abstract class VaultViewMode(string name, string viewName)
    {
        public string Name { get; } = name;
        public string ViewName { get; } = viewName;

        public string Folder => $"/ViewModes/{Name}";
        public string View => $"{Folder}/Views/{ViewName}";

        /// <summary>
        /// Optional JavaScript files required by this ViewMode.
        /// Paths should be absolute (e.g., "/ViewModes/Columns/js/columns.js").
        /// </summary>
        public virtual IReadOnlyList<string> Scripts => [];

        /// <summary>
        /// Optional CSS files required by this ViewMode.
        /// Paths should be absolute (e.g., "/ViewModes/Columns/css/vault-columns.css").
        /// </summary>
        public virtual IReadOnlyList<string> Styles => [];
    }

    public static class VaultViewModes
    {
        private static readonly List<VaultViewMode> _all = new();

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
