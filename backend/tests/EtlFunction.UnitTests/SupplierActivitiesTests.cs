using EtlFunction.Contracts;
using EtlFunction.Functions;
using EtlFunction.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EtlFunction.UnitTests;

public sealed class SupplierActivitiesTests
{
    [Fact]
    public async Task FetchSuppliersActivity_UsesSoapClientAndReturnsSuppliers()
    {
        var expected = new List<SupplierRecord>
        {
            new() { SupplierId = "S-200", Name = "Contoso" },
            new() { SupplierId = "S-201", Name = "Northwind" }
        };

        var soapClient = new FakeSoapSourceClient(expected);

        var sut = new SupplierActivities(
            supplierRepository: new FakeSupplierRepository(),
            soapSourceClient: soapClient,
            supplierValidator: new NoOpSupplierValidator(),
            changeDetectionService: new NoOpChangeDetectionService(),
            destinationApiClient: new NoOpDestinationApiClient(),
            etlMetricsService: new NoOpEtlMetricsService(),
            auditService: new NoOpAuditService(),
            retryQueueService: new NoOpRetryQueueService(),
            logger: NullLogger<SupplierActivities>.Instance);

        var runContext = new EtlRunContext { RunId = 123, TriggerSource = "UnitTest" };

        var result = await sut.FetchSuppliersActivity(runContext, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("S-200", result.First().SupplierId);
        Assert.Equal(1, soapClient.CallCount);
    }

    [Fact]
    public async Task ClassifyAndPersistActivity_ReturnsOnlyNewAndUpdated_ForDispatch()
    {
        var suppliers = new List<SupplierRecord>
        {
            new() { SupplierId = "S-300", Name = "New Supplier" },
            new() { SupplierId = "S-301", Name = "Updated Supplier" },
            new() { SupplierId = "S-302", Name = "Unchanged Supplier" }
        };

        var repository = new FakeSupplierRepository();
        repository.LastHashes["S-300"] = null;
        repository.LastHashes["S-301"] = "OLD-HASH";
        repository.LastHashes["S-302"] = "SAME-HASH";

        var changeDetection = new FakeChangeDetectionService(new Dictionary<string, SupplierChangeClassification>
        {
            ["S-300"] = SupplierChangeClassification.New,
            ["S-301"] = SupplierChangeClassification.Updated,
            ["S-302"] = SupplierChangeClassification.Unchanged
        });

        var auditService = new FakeAuditService();

        var sut = CreateSut(
            supplierRepository: repository,
            changeDetectionService: changeDetection,
            auditService: auditService);

        var result = await sut.ClassifyAndPersistActivity((999, suppliers), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Supplier.SupplierId == "S-300" && item.Classification == SupplierChangeClassification.New);
        Assert.Contains(result, item => item.Supplier.SupplierId == "S-301" && item.Classification == SupplierChangeClassification.Updated);
        Assert.DoesNotContain(result, item => item.Supplier.SupplierId == "S-302");

        Assert.Equal(3, auditService.SupplierAuditCalls.Count);
        Assert.Equal(3, repository.LastHashLookupCalls.Count);
    }

    [Fact]
    public async Task DispatchSupplierActivity_OnSuccess_WritesAuditAndClearsRetry()
    {
        var destination = new FakeDestinationApiClient(new DispatchResult
        {
            SupplierId = "S-400",
            IsSuccess = true,
            HttpStatusCode = 200,
            DurationMs = 25,
            RequestPayload = "{}",
            ResponsePayload = "{}"
        });

        var auditService = new FakeAuditService();
        var retryService = new FakeRetryQueueService();

        var sut = CreateSut(
            destinationApiClient: destination,
            auditService: auditService,
            retryQueueService: retryService);

        var item = new SupplierDispatchItem
        {
            RunId = 321,
            Supplier = new SupplierRecord { SupplierId = "S-400" },
            Classification = SupplierChangeClassification.Updated
        };

        var result = await sut.DispatchSupplierActivity((321, item), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(auditService.ApiAuditCalls);
        Assert.Single(retryService.ClearedSuppliers);
        Assert.Equal("S-400", retryService.ClearedSuppliers[0]);
        Assert.Empty(retryService.UpsertedFailures);
    }

    [Fact]
    public async Task DispatchSupplierActivity_OnFailure_WritesAuditAndUpsertsRetry()
    {
        var destination = new FakeDestinationApiClient(new DispatchResult
        {
            SupplierId = "S-401",
            IsSuccess = false,
            HttpStatusCode = 500,
            DurationMs = 30,
            FailureMessage = "HTTP 500",
            RequestPayload = "{}",
            ResponsePayload = "{\"error\":\"boom\"}"
        });

        var auditService = new FakeAuditService();
        var retryService = new FakeRetryQueueService();

        var sut = CreateSut(
            destinationApiClient: destination,
            auditService: auditService,
            retryQueueService: retryService);

        var item = new SupplierDispatchItem
        {
            RunId = 322,
            Supplier = new SupplierRecord { SupplierId = "S-401" },
            Classification = SupplierChangeClassification.Updated
        };

        var result = await sut.DispatchSupplierActivity((322, item), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Single(auditService.ApiAuditCalls);
        Assert.Single(retryService.UpsertedFailures);
        Assert.Empty(retryService.ClearedSuppliers);
        Assert.Equal("S-401", retryService.UpsertedFailures[0].SupplierId);
    }

    private static SupplierActivities CreateSut(
        ISupplierRepository? supplierRepository = null,
        ISoapSourceClient? soapSourceClient = null,
        ISupplierValidator? supplierValidator = null,
        IChangeDetectionService? changeDetectionService = null,
        IDestinationApiClient? destinationApiClient = null,
        IEtlMetricsService? etlMetricsService = null,
        IAuditService? auditService = null,
        IRetryQueueService? retryQueueService = null)
    {
        return new SupplierActivities(
            supplierRepository: supplierRepository ?? new FakeSupplierRepository(),
            soapSourceClient: soapSourceClient ?? new FakeSoapSourceClient(Array.Empty<SupplierRecord>()),
            supplierValidator: supplierValidator ?? new NoOpSupplierValidator(),
            changeDetectionService: changeDetectionService ?? new NoOpChangeDetectionService(),
            destinationApiClient: destinationApiClient ?? new NoOpDestinationApiClient(),
            etlMetricsService: etlMetricsService ?? new NoOpEtlMetricsService(),
            auditService: auditService ?? new FakeAuditService(),
            retryQueueService: retryQueueService ?? new FakeRetryQueueService(),
            logger: NullLogger<SupplierActivities>.Instance);
    }

    private sealed class FakeSoapSourceClient : ISoapSourceClient
    {
        private readonly IReadOnlyCollection<SupplierRecord> _suppliers;

        public FakeSoapSourceClient(IReadOnlyCollection<SupplierRecord> suppliers)
        {
            _suppliers = suppliers;
        }

        public int CallCount { get; private set; }

        public Task<IReadOnlyCollection<SupplierRecord>> GetSuppliersAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_suppliers);
        }
    }

    private sealed class FakeSupplierRepository : ISupplierRepository
    {
        public Dictionary<string, string?> LastHashes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> LastHashLookupCalls { get; } = [];

        public Task<long> StartRunAsync(string triggerSource, string? correlationId, CancellationToken cancellationToken) => Task.FromResult(0L);

        public Task CompleteRunAsync(long runId, string status, int totalRecords, int validRecords, int invalidRecords, int sentToApi, int apiFailures, int retryCount, long p95LatencyMs, long totalDurationMs, int slaCompliantCount, int failedBatches, decimal errorRate, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string?> GetLastSnapshotHashAsync(string supplierId, CancellationToken cancellationToken)
        {
            LastHashLookupCalls.Add(supplierId);
            LastHashes.TryGetValue(supplierId, out var value);
            return Task.FromResult(value);
        }

        public Task<bool> IsSupplierIdUniqueInDatabaseAsync(string supplierId, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<IReadOnlyCollection<string>> GetSupplierHistoryAsync(string supplierId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());

        public Task<IReadOnlyCollection<SupplierDispatchItem>> GetPendingRetriesAsync(long runId, int maxRows, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<SupplierDispatchItem>>(Array.Empty<SupplierDispatchItem>());

        public Task SetRetryStateAsync(long runId, string supplierId, bool isSuccess, string? failureReason, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoOpSupplierValidator : ISupplierValidator
    {
        public Task<(IReadOnlyCollection<SupplierRecord> Valid, IReadOnlyCollection<ValidationFailureRecord> Invalid)> ValidateAsync(long runId, IReadOnlyCollection<SupplierRecord> records, CancellationToken cancellationToken)
        {
            return Task.FromResult(((IReadOnlyCollection<SupplierRecord>)records, (IReadOnlyCollection<ValidationFailureRecord>)Array.Empty<ValidationFailureRecord>()));
        }
    }

    private sealed class NoOpChangeDetectionService : IChangeDetectionService
    {
        public SupplierChangeClassification Classify(SupplierRecord record, string? previousHash) => SupplierChangeClassification.Unchanged;

        public string ComputeRowHash(SupplierRecord record) => string.Empty;
    }

    private sealed class FakeChangeDetectionService : IChangeDetectionService
    {
        private readonly IReadOnlyDictionary<string, SupplierChangeClassification> _classifications;

        public FakeChangeDetectionService(IReadOnlyDictionary<string, SupplierChangeClassification> classifications)
        {
            _classifications = classifications;
        }

        public SupplierChangeClassification Classify(SupplierRecord record, string? previousHash)
        {
            return _classifications.TryGetValue(record.SupplierId, out var classification)
                ? classification
                : SupplierChangeClassification.Unchanged;
        }

        public string ComputeRowHash(SupplierRecord record)
        {
            return record.SupplierId == "S-302" ? "SAME-HASH" : $"HASH-{record.SupplierId}";
        }
    }

    private sealed class NoOpDestinationApiClient : IDestinationApiClient
    {
        public Task<DispatchResult> SendSupplierAsync(SupplierDispatchItem item, CancellationToken cancellationToken)
            => Task.FromResult(new DispatchResult { SupplierId = item.Supplier.SupplierId, IsSuccess = true });
    }

    private sealed class FakeDestinationApiClient : IDestinationApiClient
    {
        private readonly DispatchResult _result;

        public FakeDestinationApiClient(DispatchResult result)
        {
            _result = result;
        }

        public Task<DispatchResult> SendSupplierAsync(SupplierDispatchItem item, CancellationToken cancellationToken)
            => Task.FromResult(_result);
    }

    private sealed class NoOpEtlMetricsService : IEtlMetricsService
    {
        public EtlRunMetrics Calculate(long runId, int totalRecords, int validRecords, int invalidRecords, IReadOnlyCollection<DispatchResult> dispatchResults, DateTime startedAtUtc)
            => new();
    }

    private sealed class FakeAuditService : IAuditService
    {
        public List<ValidationFailureRecord> ValidationAuditCalls { get; } = [];

        public List<(SupplierRecord Record, SupplierChangeClassification Classification)> SupplierAuditCalls { get; } = [];

        public List<DispatchResult> ApiAuditCalls { get; } = [];

        public Task WriteValidationAuditAsync(long runId, ValidationFailureRecord failure, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task WriteSupplierAuditAsync(long runId, SupplierRecord record, SupplierChangeClassification classification, CancellationToken cancellationToken)
        {
            SupplierAuditCalls.Add((record, classification));
            return Task.CompletedTask;
        }

        public Task WriteApiAuditAsync(long runId, DispatchResult dispatchResult, CancellationToken cancellationToken)
        {
            ApiAuditCalls.Add(dispatchResult);
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpAuditService : IAuditService
    {
        public Task WriteValidationAuditAsync(long runId, ValidationFailureRecord failure, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task WriteSupplierAuditAsync(long runId, SupplierRecord record, SupplierChangeClassification classification, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task WriteApiAuditAsync(long runId, DispatchResult dispatchResult, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeRetryQueueService : IRetryQueueService
    {
        public List<DispatchResult> UpsertedFailures { get; } = [];

        public List<string> ClearedSuppliers { get; } = [];

        public Task<IReadOnlyCollection<SupplierDispatchItem>> GetPendingRetriesAsync(long runId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<SupplierDispatchItem>>(Array.Empty<SupplierDispatchItem>());

        public Task UpsertRetryAsync(long runId, DispatchResult dispatchResult, CancellationToken cancellationToken)
        {
            UpsertedFailures.Add(dispatchResult);
            return Task.CompletedTask;
        }

        public Task ClearRetryAsync(string supplierId, CancellationToken cancellationToken)
        {
            ClearedSuppliers.Add(supplierId);
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpRetryQueueService : IRetryQueueService
    {
        public Task<IReadOnlyCollection<SupplierDispatchItem>> GetPendingRetriesAsync(long runId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<SupplierDispatchItem>>(Array.Empty<SupplierDispatchItem>());

        public Task UpsertRetryAsync(long runId, DispatchResult dispatchResult, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ClearRetryAsync(string supplierId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
