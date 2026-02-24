namespace EtlFunction.Configuration;

/// <summary>
/// Destination API client configuration.
/// </summary>
public sealed class DestinationApiOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "DestinationApi";

    /// <summary>
    /// Destination API base URL.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Destination API key or token reference.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
