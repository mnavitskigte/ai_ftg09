namespace EtlFunction.Models;

/// <summary>
/// Internal supplier domain model.
/// </summary>
public sealed class SupplierRecord
{
    /// <summary>
    /// Source supplier identifier.
    /// </summary>
    public string SupplierId { get; set; } = string.Empty;

    /// <summary>
    /// Supplier display name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Bank account name.
    /// </summary>
    public string? BankAccountName { get; set; }

    /// <summary>
    /// Bank account number.
    /// </summary>
    public string? BankAccountNumber { get; set; }

    /// <summary>
    /// Bank routing number.
    /// </summary>
    public string? BankRoutingNumber { get; set; }

    /// <summary>
    /// Address line 1.
    /// </summary>
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// City.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Country code.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Raw source payload for diagnostics.
    /// </summary>
    public string? RawPayload { get; set; }

    /// <summary>
    /// TODO: inject schema for all supplier fields.
    /// </summary>
    public Dictionary<string, string?> AdditionalFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
