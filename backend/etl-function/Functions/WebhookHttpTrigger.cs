using EtlFunction.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace EtlFunction.Functions;

/// <summary>
/// HTTP webhook trigger entry point for supplier ETL orchestration.
/// </summary>
public sealed class WebhookHttpTrigger
{
    private readonly ILogger<WebhookHttpTrigger> _logger;

    /// <summary>
    /// Creates a new <see cref="WebhookHttpTrigger"/> instance.
    /// </summary>
    public WebhookHttpTrigger(ILogger<WebhookHttpTrigger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts durable orchestration for incoming webhook callback.
    /// </summary>
    [Function(nameof(WebhookHttpTrigger))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "etl/suppliers/webhook")] HttpRequestData request,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Supplier webhook received. Starting orchestration.");

        var input = new EtlRunContext
        {
            TriggerSource = "SupplierWebhook",
            CorrelationId = context.InvocationId,
            StartedAtUtc = DateTime.UtcNow
        };

        var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(SupplierEtlOrchestrator),
            input,
            cancellationToken);

        var response = request.CreateResponse(System.Net.HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { instanceId }, cancellationToken);
        return response;
    }
}
