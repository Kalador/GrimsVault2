namespace Vault.Core
{
    /// <summary>
    /// Class used to prep and direct the breathing loader. 
    /// Passed to the loader partial to pre-render output.
    /// </summary>
    public class LoaderModel
    {
        /// <summary>
        /// Message displayed in loading screen
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// URL the loader POSTs back to
        /// </summary>
        public string? TargetUrl { get; set; }

        /// <summary>
        /// Internal action name sent via X-Loader-Action header
        /// </summary>
        public string? TargetAction { get; set; }
    }
}
