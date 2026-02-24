using EtlFunction.Functions;
using EtlFunction.Models;
using Microsoft.DurableTask;
using Moq;
using Xunit;

namespace EtlFunction.UnitTests;

public sealed class SupplierEtlOrchestratorTests
{
    [Fact]
    public async Task RunAsync_CallsActivitiesInExpectedOrder_AndReturnsMetrics()
    {
        var sequence = new MockSequence();
        var context = new Mock<TaskOrchestrationContext>(MockBehavior.Strict);

        var input = new EtlRunContext { TriggerSource = "UnitTest", CorrelationId = "corr-1" };
        var started = new EtlRunContext { RunId = 101, TriggerSource = "UnitTest", CorrelationId = "corr-1" };

        var fetched = new List<SupplierRecord>
        {
            new() { SupplierId = "S-1", Name = "One" },
            new() { SupplierId = "S-2", Name = "Two" }
        };

        var valid = new List<SupplierRecord>
        {
            fetched[0],
            fetched[1]
        };

        var invalid = new List<ValidationFailureRecord>();

        var classified = new List<SupplierDispatchItem>
        {
            new() { RunId = 101, Supplier = fetched[0], Classification = SupplierChangeClassification.New }
        };

        var retry = new List<SupplierDispatchItem>
        {
            new() { RunId = 101, Supplier = fetched[1], Classification = SupplierChangeClassification.Updated }
        };

        var dispatch1 = new DispatchResult { SupplierId = "S-1", IsSuccess = true };
        var dispatch2 = new DispatchResult { SupplierId = "S-2", IsSuccess = false, FailureMessage = "boom" };

        var expectedMetrics = new EtlRunMetrics
        {
            TotalRecords = 2,
            ValidRecords = 2,
            InvalidRecords = 0,
            SentToApi = 2,
            ApiFailures = 1
        };

        context.InSequence(sequence)
            .Setup(c => c.GetInput<EtlRunContext>())
            .Returns(input);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<EtlRunContext>(
                nameof(SupplierActivities.StartRunActivity),
                It.IsAny<EtlRunContext>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(started);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierRecord>>(
                nameof(SupplierActivities.FetchSuppliersActivity),
                It.IsAny<EtlRunContext>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(fetched);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<(IReadOnlyCollection<SupplierRecord> Valid, IReadOnlyCollection<ValidationFailureRecord> Invalid)>(
                nameof(SupplierActivities.ValidateSuppliersActivity),
                It.IsAny<(long, IReadOnlyCollection<SupplierRecord>)>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync((valid, invalid));

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
                nameof(SupplierActivities.ClassifyAndPersistActivity),
                It.IsAny<(long, IReadOnlyCollection<SupplierRecord>)>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(classified);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
                nameof(SupplierActivities.LoadPendingRetryActivity),
                It.IsAny<long>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(retry);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<DispatchResult>(
                nameof(SupplierActivities.DispatchSupplierActivity),
                It.Is<(long, SupplierDispatchItem)>(x => x.Item2.Supplier.SupplierId == "S-1"),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(dispatch1);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<DispatchResult>(
                nameof(SupplierActivities.DispatchSupplierActivity),
                It.Is<(long, SupplierDispatchItem)>(x => x.Item2.Supplier.SupplierId == "S-2"),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(dispatch2);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<EtlRunMetrics>(
                nameof(SupplierActivities.CalculateAndPersistMetricsActivity),
                It.Is<(EtlRunContext, int, int, int, IReadOnlyCollection<DispatchResult>)>(x =>
                    x.Item1.RunId == 101 && x.Item2 == 2 && x.Item3 == 2 && x.Item4 == 0 && x.Item5.Count == 2),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(expectedMetrics);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync(
                nameof(SupplierActivities.CompleteRunActivity),
                It.Is<(long, EtlRunMetrics)>(x => x.Item1 == 101 && x.Item2 == expectedMetrics),
                It.IsAny<TaskOptions?>()))
            .Returns(Task.CompletedTask);

        var result = await SupplierEtlOrchestrator.RunAsync(context.Object);

        Assert.Same(expectedMetrics, result);

        context.VerifyAll();
    }

    [Fact]
    public async Task RunAsync_WhenInputIsNull_UsesDefaultRunContext()
    {
        var sequence = new MockSequence();
        var context = new Mock<TaskOrchestrationContext>(MockBehavior.Strict);

        var started = new EtlRunContext { RunId = 202, TriggerSource = "Fallback" };
        var fetched = Array.Empty<SupplierRecord>();
        var valid = Array.Empty<SupplierRecord>();
        var invalid = Array.Empty<ValidationFailureRecord>();
        var classified = Array.Empty<SupplierDispatchItem>();
        var retry = Array.Empty<SupplierDispatchItem>();
        var expectedMetrics = new EtlRunMetrics
        {
            TotalRecords = 0,
            ValidRecords = 0,
            InvalidRecords = 0,
            SentToApi = 0,
            ApiFailures = 0
        };

        context.InSequence(sequence)
            .Setup(c => c.GetInput<EtlRunContext>())
            .Returns((EtlRunContext?)null);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<EtlRunContext>(
                nameof(SupplierActivities.StartRunActivity),
                It.Is<EtlRunContext>(x =>
                    x.RunId == 0 &&
                    x.TriggerSource == string.Empty &&
                    x.CorrelationId == null &&
                    x.Status == "Running"),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(started);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierRecord>>(
                nameof(SupplierActivities.FetchSuppliersActivity),
                started,
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(fetched);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<(IReadOnlyCollection<SupplierRecord> Valid, IReadOnlyCollection<ValidationFailureRecord> Invalid)>(
                nameof(SupplierActivities.ValidateSuppliersActivity),
                It.Is<(long, IReadOnlyCollection<SupplierRecord>)>(x => x.Item1 == 202 && x.Item2.Count == 0),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync((valid, invalid));

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
                nameof(SupplierActivities.ClassifyAndPersistActivity),
                It.Is<(long, IReadOnlyCollection<SupplierRecord>)>(x => x.Item1 == 202 && x.Item2.Count == 0),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(classified);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
                nameof(SupplierActivities.LoadPendingRetryActivity),
                202L,
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(retry);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync<EtlRunMetrics>(
                nameof(SupplierActivities.CalculateAndPersistMetricsActivity),
                It.Is<(EtlRunContext, int, int, int, IReadOnlyCollection<DispatchResult>)>(x =>
                    x.Item1.RunId == 202 && x.Item2 == 0 && x.Item3 == 0 && x.Item4 == 0 && x.Item5.Count == 0),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(expectedMetrics);

        context.InSequence(sequence)
            .Setup(c => c.CallActivityAsync(
                nameof(SupplierActivities.CompleteRunActivity),
                It.Is<(long, EtlRunMetrics)>(x => x.Item1 == 202 && x.Item2 == expectedMetrics),
                It.IsAny<TaskOptions?>()))
            .Returns(Task.CompletedTask);

        var result = await SupplierEtlOrchestrator.RunAsync(context.Object);

        Assert.Same(expectedMetrics, result);

        context.VerifyAll();
    }

    [Fact]
    public async Task RunAsync_WhenClassifiedAndRetryContainSameSupplier_DispatchesOnceWithClassifiedPrecedence()
    {
        var context = new Mock<TaskOrchestrationContext>(MockBehavior.Strict);

        var input = new EtlRunContext { TriggerSource = "UnitTest", CorrelationId = "corr-dup" };
        var started = new EtlRunContext { RunId = 303, TriggerSource = "UnitTest", CorrelationId = "corr-dup" };

        var supplier = new SupplierRecord { SupplierId = "S-DUP", Name = "Duplicate Supplier" };
        var fetched = new List<SupplierRecord> { supplier };
        var valid = new List<SupplierRecord> { supplier };
        var invalid = Array.Empty<ValidationFailureRecord>();

        var classified = new List<SupplierDispatchItem>
        {
            new() { RunId = 303, Supplier = supplier, Classification = SupplierChangeClassification.Updated }
        };

        var retry = new List<SupplierDispatchItem>
        {
            new() { RunId = 303, Supplier = supplier, Classification = SupplierChangeClassification.Updated, IsRetry = true }
        };

        var dispatchResult = new DispatchResult { SupplierId = "S-DUP", IsSuccess = true };

        var expectedMetrics = new EtlRunMetrics
        {
            TotalRecords = 1,
            ValidRecords = 1,
            InvalidRecords = 0,
            SentToApi = 1,
            ApiFailures = 0
        };

        context.Setup(c => c.GetInput<EtlRunContext>())
            .Returns(input);

        context.Setup(c => c.CallActivityAsync<EtlRunContext>(
                nameof(SupplierActivities.StartRunActivity),
                It.IsAny<EtlRunContext>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(started);

        context.Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierRecord>>(
                nameof(SupplierActivities.FetchSuppliersActivity),
                started,
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(fetched);

        context.Setup(c => c.CallActivityAsync<(IReadOnlyCollection<SupplierRecord> Valid, IReadOnlyCollection<ValidationFailureRecord> Invalid)>(
                nameof(SupplierActivities.ValidateSuppliersActivity),
                It.IsAny<(long, IReadOnlyCollection<SupplierRecord>)>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync((valid, invalid));

        context.Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
                nameof(SupplierActivities.ClassifyAndPersistActivity),
                It.IsAny<(long, IReadOnlyCollection<SupplierRecord>)>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(classified);

        context.Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
                nameof(SupplierActivities.LoadPendingRetryActivity),
                303L,
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(retry);

        context.Setup(c => c.CallActivityAsync<DispatchResult>(
                nameof(SupplierActivities.DispatchSupplierActivity),
                It.Is<(long, SupplierDispatchItem)>(x => x.Item1 == 303 && x.Item2.Supplier.SupplierId == "S-DUP"),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(dispatchResult);

        context.Setup(c => c.CallActivityAsync<EtlRunMetrics>(
                nameof(SupplierActivities.CalculateAndPersistMetricsActivity),
                It.Is<(EtlRunContext, int, int, int, IReadOnlyCollection<DispatchResult>)>(x =>
                    x.Item1.RunId == 303 && x.Item2 == 1 && x.Item3 == 1 && x.Item4 == 0 && x.Item5.Count == 1),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(expectedMetrics);

        context.Setup(c => c.CallActivityAsync(
                nameof(SupplierActivities.CompleteRunActivity),
                It.Is<(long, EtlRunMetrics)>(x => x.Item1 == 303 && x.Item2 == expectedMetrics),
                It.IsAny<TaskOptions?>()))
            .Returns(Task.CompletedTask);

        var result = await SupplierEtlOrchestrator.RunAsync(context.Object);

        Assert.Same(expectedMetrics, result);
        context.Verify(c => c.CallActivityAsync<DispatchResult>(
            nameof(SupplierActivities.DispatchSupplierActivity),
            It.Is<(long, SupplierDispatchItem)>(x => x.Item1 == 303 && x.Item2.Supplier.SupplierId == "S-DUP" && !x.Item2.IsRetry),
            It.IsAny<TaskOptions?>()), Times.Once);
        context.Verify(c => c.CallActivityAsync<DispatchResult>(
            nameof(SupplierActivities.DispatchSupplierActivity),
            It.Is<(long, SupplierDispatchItem)>(x => x.Item1 == 303 && x.Item2.Supplier.SupplierId == "S-DUP" && x.Item2.IsRetry),
            It.IsAny<TaskOptions?>()), Times.Never);
        context.VerifyAll();
    }

    [Fact]
    public async Task RunAsync_DeduplicatesSupplierIds_CaseInsensitive()
    {
        var context = new Mock<TaskOrchestrationContext>(MockBehavior.Strict);

        var input = new EtlRunContext { TriggerSource = "UnitTest", CorrelationId = "corr-case" };
        var started = new EtlRunContext { RunId = 304, TriggerSource = "UnitTest", CorrelationId = "corr-case" };

        var classifiedSupplier = new SupplierRecord { SupplierId = "S-100", Name = "Supplier Upper" };
        var retrySupplier = new SupplierRecord { SupplierId = "s-100", Name = "Supplier Lower" };

        var fetched = new List<SupplierRecord> { classifiedSupplier };
        var valid = new List<SupplierRecord> { classifiedSupplier };
        var invalid = Array.Empty<ValidationFailureRecord>();

        var classified = new List<SupplierDispatchItem>
        {
            new() { RunId = 304, Supplier = classifiedSupplier, Classification = SupplierChangeClassification.Updated }
        };

        var retry = new List<SupplierDispatchItem>
        {
            new() { RunId = 304, Supplier = retrySupplier, Classification = SupplierChangeClassification.Updated, IsRetry = true }
        };

        var dispatchResult = new DispatchResult { SupplierId = "S-100", IsSuccess = true };

        var expectedMetrics = new EtlRunMetrics
        {
            TotalRecords = 1,
            ValidRecords = 1,
            InvalidRecords = 0,
            SentToApi = 1,
            ApiFailures = 0
        };

        context.Setup(c => c.GetInput<EtlRunContext>())
            .Returns(input);

        context.Setup(c => c.CallActivityAsync<EtlRunContext>(
                nameof(SupplierActivities.StartRunActivity),
                It.IsAny<EtlRunContext>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(started);

        context.Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierRecord>>(
                nameof(SupplierActivities.FetchSuppliersActivity),
                started,
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(fetched);

        context.Setup(c => c.CallActivityAsync<(IReadOnlyCollection<SupplierRecord> Valid, IReadOnlyCollection<ValidationFailureRecord> Invalid)>(
                nameof(SupplierActivities.ValidateSuppliersActivity),
                It.IsAny<(long, IReadOnlyCollection<SupplierRecord>)>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync((valid, invalid));

        context.Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
                nameof(SupplierActivities.ClassifyAndPersistActivity),
                It.IsAny<(long, IReadOnlyCollection<SupplierRecord>)>(),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(classified);

        context.Setup(c => c.CallActivityAsync<IReadOnlyCollection<SupplierDispatchItem>>(
                nameof(SupplierActivities.LoadPendingRetryActivity),
                304L,
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(retry);

        context.Setup(c => c.CallActivityAsync<DispatchResult>(
                nameof(SupplierActivities.DispatchSupplierActivity),
                It.Is<(long, SupplierDispatchItem)>(x => x.Item1 == 304 && x.Item2.Supplier.SupplierId == "S-100" && !x.Item2.IsRetry),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(dispatchResult);

        context.Setup(c => c.CallActivityAsync<EtlRunMetrics>(
                nameof(SupplierActivities.CalculateAndPersistMetricsActivity),
                It.Is<(EtlRunContext, int, int, int, IReadOnlyCollection<DispatchResult>)>(x =>
                    x.Item1.RunId == 304 && x.Item2 == 1 && x.Item3 == 1 && x.Item4 == 0 && x.Item5.Count == 1),
                It.IsAny<TaskOptions?>()))
            .ReturnsAsync(expectedMetrics);

        context.Setup(c => c.CallActivityAsync(
                nameof(SupplierActivities.CompleteRunActivity),
                It.Is<(long, EtlRunMetrics)>(x => x.Item1 == 304 && x.Item2 == expectedMetrics),
                It.IsAny<TaskOptions?>()))
            .Returns(Task.CompletedTask);

        var result = await SupplierEtlOrchestrator.RunAsync(context.Object);

        Assert.Same(expectedMetrics, result);
        context.Verify(c => c.CallActivityAsync<DispatchResult>(
            nameof(SupplierActivities.DispatchSupplierActivity),
            It.Is<(long, SupplierDispatchItem)>(x => x.Item1 == 304 && x.Item2.Supplier.SupplierId == "S-100" && !x.Item2.IsRetry),
            It.IsAny<TaskOptions?>()), Times.Once);
        context.Verify(c => c.CallActivityAsync<DispatchResult>(
            nameof(SupplierActivities.DispatchSupplierActivity),
            It.Is<(long, SupplierDispatchItem)>(x => x.Item1 == 304 && x.Item2.Supplier.SupplierId == "s-100" && x.Item2.IsRetry),
            It.IsAny<TaskOptions?>()), Times.Never);
        context.VerifyAll();
    }
}
