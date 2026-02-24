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
}
