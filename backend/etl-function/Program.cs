using EtlFunction.Clients;
using EtlFunction.Configuration;
using EtlFunction.Contracts;
using EtlFunction.Models;
using EtlFunction.Repositories;
using EtlFunction.Services;
using EtlFunction.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console();
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddOptions<SoapClientOptions>()
            .BindConfiguration(SoapClientOptions.SectionName);

        services.AddOptions<DestinationApiOptions>()
            .BindConfiguration(DestinationApiOptions.SectionName);

        services.AddHttpClient<IDestinationApiClient, DestinationApiClient>()
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(message => (int)message.StatusCode == 408)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        services.AddHttpClient<ISoapSourceClient, SoapSourceClient>();

        services.AddScoped<ISupplierValidator, SupplierValidator>();
        services.AddScoped<IValidator<SupplierRecord>, SupplierRecordValidator>();
        services.AddScoped<IChangeDetectionService, ChangeDetectionService>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IAuditRepository, SupplierRepository>();
        services.AddScoped<IEtlMetricsService, EtlMetricsService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IRetryQueueService, RetryQueueService>();
    })
    .Build();

await host.RunAsync();
