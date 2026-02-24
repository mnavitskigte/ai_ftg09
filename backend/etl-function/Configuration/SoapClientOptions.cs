namespace EtlFunction.Configuration;

/// <summary>
/// SOAP client configuration.
/// </summary>
public sealed class SoapClientOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "SoapClient";

    /// <summary>
    /// SOAP service endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// SOAP username from Key Vault-backed app settings.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SOAP password from Key Vault-backed app settings.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// SOAPAction header value.
    /// </summary>
    public string? SoapAction { get; set; }

    /// <summary>
    /// Request XML namespace for operation body.
    /// </summary>
    public string RequestNamespace { get; set; } = "urn:supplier-service";

    /// <summary>
    /// Request operation element name.
    /// </summary>
    public string RequestOperationName { get; set; } = "GetSuppliersRequest";

    /// <summary>
    /// Comma-separated supplier node names in SOAP response.
    /// </summary>
    public string SupplierNodeNames { get; set; } = "Supplier,SupplierRecord,Vendor";
}
