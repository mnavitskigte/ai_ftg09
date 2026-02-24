using System.Net.Http.Json;
using System.Text.Json;
using EtlFunction.Contracts;
using EtlFunction.Models;
using Microsoft.Extensions.Logging;

namespace EtlFunction.Clients;

/// <summary>
/// Destination API client implementation.
/// </summary>
public sealed class DestinationApiClient : IDestinationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DestinationApiClient> _logger;

    /// <summary>
    /// Creates a new <see cref="DestinationApiClient"/> instance.
    /// </summary>
    public DestinationApiClient(HttpClient httpClient, ILogger<DestinationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DispatchResult> SendSupplierAsync(SupplierDispatchItem item, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Dispatching supplier {SupplierId} to destination API", item.Supplier.SupplierId);

        var startedAt = DateTime.UtcNow;
        var requestPayload = JsonSerializer.Serialize(item.Supplier);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/suppliers", item.Supplier, cancellationToken);
            var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);

            return new DispatchResult
            {
                SupplierId = item.Supplier.SupplierId,
                IsSuccess = response.IsSuccessStatusCode,
                HttpStatusCode = (int)response.StatusCode,
                FailureMessage = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                DurationMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds,
                RequestPayload = requestPayload,
                ResponsePayload = responsePayload
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Destination API call failed for supplier {SupplierId}", item.Supplier.SupplierId);

            return new DispatchResult
            {
                SupplierId = item.Supplier.SupplierId,
                IsSuccess = false,
                HttpStatusCode = null,
                FailureMessage = ex.Message,
                DurationMs = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds,
                RequestPayload = requestPayload,
                ResponsePayload = null
            };
        }
    }
}
