namespace ReqRes.Client.DTOs
{
    /// <summary>
    /// Represents configuration options for making requests to the ReqRes API.
    /// </summary>
    /// <remarks>This class provides properties to configure the base URL, timeout duration, and API key
    /// required for interacting with the ReqRes API. These options are typically used to initialize an API client or
    /// service.</remarks>
    public class ReqResOptions
    {
        /// <summary>
        /// Gets or sets the base URL used for API requests.
        /// </summary>
        /// <remarks>Ensure that the value assigned to this property is a well-formed absolute URI. This
        /// property is typically used to configure the endpoint for HTTP requests.</remarks>
        public string BaseUrl { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the timeout duration, in seconds, for the operation.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;
        /// <summary>
        /// Gets or sets the API key used for authenticating requests.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }
}