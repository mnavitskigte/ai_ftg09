using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Xml.Linq;
using EtlFunction.Configuration;
using EtlFunction.Contracts;
using EtlFunction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EtlFunction.Clients;

/// <summary>
/// SOAP source client implementation.
/// </summary>
public sealed class SoapSourceClient : ISoapSourceClient
{
    private static readonly string[] DefaultSupplierNodeNames = ["Supplier", "SupplierRecord", "Vendor"];

    private readonly HttpClient _httpClient;
    private readonly SoapClientOptions _options;
    private readonly ILogger<SoapSourceClient> _logger;

    /// <summary>
    /// Creates a new <see cref="SoapSourceClient"/> instance.
    /// </summary>
    public SoapSourceClient(
        HttpClient httpClient,
        IOptions<SoapClientOptions> options,
        ILogger<SoapSourceClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SupplierRecord>> GetSuppliersAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("SOAP endpoint is not configured.");
        }

        _logger.LogInformation("Fetching full supplier list from SOAP source endpoint {Endpoint}", _options.Endpoint);

        var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = new StringContent(BuildEnvelope(), Encoding.UTF8, "text/xml")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
        if (!string.IsNullOrWhiteSpace(_options.SoapAction))
        {
            request.Headers.TryAddWithoutValidation("SOAPAction", _options.SoapAction);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var suppliers = ParseSuppliers(payload, ResolveSupplierNodeNames());
        _logger.LogInformation("Fetched {SupplierCount} supplier records from SOAP source", suppliers.Count);
        return suppliers;
    }

    private string BuildEnvelope()
    {
        return $"""
               <?xml version="1.0" encoding="utf-8"?>
                             <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:sup="{SecurityElement.Escape(_options.RequestNamespace) ?? "urn:supplier-service"}">
                 <soapenv:Header/>
                 <soapenv:Body>
                                     <sup:{SecurityElement.Escape(_options.RequestOperationName) ?? "GetSuppliersRequest"}>
                     <sup:Username>{SecurityElement.Escape(_options.Username) ?? string.Empty}</sup:Username>
                     <sup:Password>{SecurityElement.Escape(_options.Password) ?? string.Empty}</sup:Password>
                                     </sup:{SecurityElement.Escape(_options.RequestOperationName) ?? "GetSuppliersRequest"}>
                 </soapenv:Body>
               </soapenv:Envelope>
               """;
    }

        private static IReadOnlyCollection<SupplierRecord> ParseSuppliers(string xml, IReadOnlyCollection<string> supplierNodeNames)
    {
        var document = XDocument.Parse(xml);

        var supplierNodes = document
            .Descendants()
                        .Where(element => supplierNodeNames.Contains(element.Name.LocalName, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        var results = new List<SupplierRecord>(supplierNodes.Length);

        foreach (var node in supplierNodes)
        {
            var record = new SupplierRecord
            {
                SupplierId = ReadValue(node, "SupplierId", "Id", "VendorId") ?? string.Empty,
                Name = ReadValue(node, "Name", "SupplierName", "VendorName", "LegalName"),
                BankAccountName = ReadValue(node, "BankAccountName", "AccountName"),
                BankAccountNumber = ReadValue(node, "BankAccountNumber", "AccountNumber"),
                BankRoutingNumber = ReadValue(node, "BankRoutingNumber", "RoutingNumber", "SortCode"),
                AddressLine1 = ReadValue(node, "AddressLine1", "Street", "Street1"),
                City = ReadValue(node, "City", "Town"),
                CountryCode = ReadValue(node, "CountryCode", "Country"),
                RawPayload = node.ToString(SaveOptions.DisableFormatting)
            };

            foreach (var leaf in node.Descendants().Where(d => !d.HasElements))
            {
                var key = leaf.Name.LocalName;
                var value = leaf.Value;
                if (!record.AdditionalFields.ContainsKey(key))
                {
                    record.AdditionalFields[key] = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                }
            }

            results.Add(record);
        }

        return results;
    }

    private static string? ReadValue(XElement node, params string[] elementNames)
    {
        foreach (var name in elementNames)
        {
            var value = node
                .Descendants()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private IReadOnlyCollection<string> ResolveSupplierNodeNames()
    {
        if (string.IsNullOrWhiteSpace(_options.SupplierNodeNames))
        {
            return DefaultSupplierNodeNames;
        }

        var names = _options.SupplierNodeNames
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return names.Length > 0 ? names : DefaultSupplierNodeNames;
    }
}
