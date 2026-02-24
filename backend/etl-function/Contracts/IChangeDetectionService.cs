using EtlFunction.Models;

namespace EtlFunction.Contracts;

/// <summary>
/// Change detection service abstraction.
/// </summary>
public interface IChangeDetectionService
{
    /// <summary>
    /// Classifies supplier change state.
    /// </summary>
    /// <param name="record">Supplier record.</param>
    /// <param name="previousHash">Previous persisted hash.</param>
    /// <returns>Change classification.</returns>
    SupplierChangeClassification Classify(SupplierRecord record, string? previousHash);

    /// <summary>
    /// Computes normalized row hash for supplier record.
    /// </summary>
    /// <param name="record">Supplier record.</param>
    /// <returns>SHA256 hash string.</returns>
    string ComputeRowHash(SupplierRecord record);
}
