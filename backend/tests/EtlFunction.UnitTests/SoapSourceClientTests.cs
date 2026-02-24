using System.Net;
using System.Text;
using EtlFunction.Clients;
using EtlFunction.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EtlFunction.UnitTests;

public sealed class SoapSourceClientTests
{
    [Fact]
    public async Task GetSuppliersAsync_ParsesSupplierNodes_ToSupplierRecords()
    {
        var xml = """
                  <?xml version="1.0" encoding="utf-8"?>
                  <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
                    <soapenv:Body>
                      <GetSuppliersResponse>
                        <Suppliers>
                          <Supplier>
                            <SupplierId>S-100</SupplierId>
                            <Name>Acme GmbH</Name>
                            <BankAccountName>Main Account</BankAccountName>
                            <BankAccountNumber>123456</BankAccountNumber>
                            <BankRoutingNumber>BR-10</BankRoutingNumber>
                            <AddressLine1>Street 1</AddressLine1>
                            <City>Berlin</City>
                            <CountryCode>DE</CountryCode>
                          </Supplier>
                          <SupplierRecord>
                            <Id>S-101</Id>
                            <LegalName>Globex LLC</LegalName>
                            <AccountName>Ops</AccountName>
                            <AccountNumber>9999</AccountNumber>
                            <RoutingNumber>BR-11</RoutingNumber>
                            <Street1>2nd Ave</Street1>
                            <Town>Boston</Town>
                            <Country>US</Country>
                          </SupplierRecord>
                        </Suppliers>
                      </GetSuppliersResponse>
                    </soapenv:Body>
                  </soapenv:Envelope>
                  """;

        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(xml, Encoding.UTF8, "text/xml")
            }));

        var httpClient = new HttpClient(handler);
        var options = Options.Create(new SoapClientOptions
        {
            Endpoint = "https://supplier.local/soap",
            Username = "user",
            Password = "pass",
            RequestNamespace = "urn:supplier-service",
            RequestOperationName = "GetSuppliersRequest",
            SupplierNodeNames = "Supplier,SupplierRecord"
        });

        var sut = new SoapSourceClient(httpClient, options, NullLogger<SoapSourceClient>.Instance);

        var result = await sut.GetSuppliersAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);

        var first = result.ElementAt(0);
        Assert.Equal("S-100", first.SupplierId);
        Assert.Equal("Acme GmbH", first.Name);
        Assert.Equal("DE", first.CountryCode);
        Assert.False(string.IsNullOrWhiteSpace(first.RawPayload));

        var second = result.ElementAt(1);
        Assert.Equal("S-101", second.SupplierId);
        Assert.Equal("Globex LLC", second.Name);
        Assert.Equal("US", second.CountryCode);
        Assert.Contains("LegalName", second.AdditionalFields.Keys);
    }

    [Fact]
    public async Task GetSuppliersAsync_WhenSoapReturnsNonSuccess_ThrowsHttpRequestException()
    {
      var handler = new StubHttpMessageHandler(_ =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
          Content = new StringContent("<error>Upstream unavailable</error>", Encoding.UTF8, "text/xml")
        }));

      var httpClient = new HttpClient(handler);
      var options = Options.Create(new SoapClientOptions
      {
        Endpoint = "https://supplier.local/soap",
        Username = "user",
        Password = "pass"
      });

      var sut = new SoapSourceClient(httpClient, options, NullLogger<SoapSourceClient>.Instance);

      await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetSuppliersAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetSuppliersAsync_WhenXmlIsMalformed_ThrowsXmlException()
    {
      var malformedXml = "<soapenv:Envelope><soapenv:Body><Supplier></soapenv:Body></soapenv:Envelope>";

      var handler = new StubHttpMessageHandler(_ =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(malformedXml, Encoding.UTF8, "text/xml")
        }));

      var httpClient = new HttpClient(handler);
      var options = Options.Create(new SoapClientOptions
      {
        Endpoint = "https://supplier.local/soap",
        Username = "user",
        Password = "pass"
      });

      var sut = new SoapSourceClient(httpClient, options, NullLogger<SoapSourceClient>.Instance);

      await Assert.ThrowsAsync<System.Xml.XmlException>(() => sut.GetSuppliersAsync(CancellationToken.None));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
