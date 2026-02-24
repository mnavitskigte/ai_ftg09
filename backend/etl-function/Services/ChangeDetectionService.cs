using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EtlFunction.Contracts;
using EtlFunction.Models;

namespace EtlFunction.Services;

/// <summary>
/// Change detection service based on row hash.
/// </summary>
public sealed class ChangeDetectionService : IChangeDetectionService
{
    /// <inheritdoc />
    public SupplierChangeClassification Classify(SupplierRecord record, string? previousHash)
    {
        var currentHash = ComputeRowHash(record);

        if (string.IsNullOrWhiteSpace(previousHash))
        {
            return SupplierChangeClassification.New;
        }

        return string.Equals(currentHash, previousHash, StringComparison.OrdinalIgnoreCase)
            ? SupplierChangeClassification.Unchanged
            : SupplierChangeClassification.Updated;
    }

    /// <inheritdoc />
    public string ComputeRowHash(SupplierRecord record)
    {
        var payload = JsonSerializer.Serialize(record);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
