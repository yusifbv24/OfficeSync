namespace FileService.Infrastructure.Configurations
{
    /// <summary>
    /// Configuration class for service endpoint URLs.
    /// These values are read from appsettings.json and injected into service clients.
    /// 
    /// In production, these would point to the actual service URLs in your network:
    /// - Development: http://localhost:5001, http://localhost:5002, etc.
    /// - Production: http://user-service.local, http://channel-service.local, etc.
    /// 
    /// Using configuration makes it easy to change service URLs without recompiling code.
    /// </summary>
    public record ServiceEndpoints
    {
        /// <summary>
        /// Base URL for the User Management Service API.
        /// Example: "http://localhost:5001" or "http://user-service.local"
        /// </summary>
        public string UserServiceUrl { get; set; } = string.Empty;

        /// <summary>
        /// Base URL for the Channel Service API.
        /// Example: "http://localhost:5002" or "http://channel-service.local"
        /// </summary>
        public string ChannelServiceUrl { get; set; } = string.Empty;
    }
}