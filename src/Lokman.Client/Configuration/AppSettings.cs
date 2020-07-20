namespace Lokman.Client
{
    /// <summary>
    /// General application settings
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Development / Production
        /// </summary>
        public string Environment { get; set; } = "Production";

        /// <summary>
        /// Url for grpc and other
        /// </summary>
        public string ServedUrl { get; set; } = "https://localhost:1337/";

        /// <summary>
        /// Enable or disable livereload component in layout
        /// </summary>
        public bool EnableLiveReload { get; set; }
    }
}
