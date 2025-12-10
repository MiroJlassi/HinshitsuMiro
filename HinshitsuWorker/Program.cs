using HinshitsuWorker;
using HinshitsuWorker.Services;
using Polly;
using Polly.Extensions.Http;

var builder = Host.CreateApplicationBuilder(args);

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetryWorkerService();

builder.Services.AddSingleton<QueueProcessor>();
builder.Services.AddSingleton<JobUpdateService>();
builder.Services.AddSingleton<BlobStorageService>();

builder.Services.AddHttpClient<NotificationService>();
builder.Services.AddSingleton<NotificationService>();

// Get HTTP client settings from configuration
var timeoutSeconds = builder.Configuration.GetValue<int>("HttpClientSettings:TimeoutSeconds", 1000);
var retryCount = builder.Configuration.GetValue<int>("HttpClientSettings:RetryCount", 3);
var retryDelaySeconds = builder.Configuration.GetValue<int>("HttpClientSettings:RetryDelaySeconds", 2);

Console.WriteLine($"[CONFIG] HttpClient Timeout: {timeoutSeconds}s, Retry Count: {retryCount}, Retry Delay: {retryDelaySeconds}s");

// Create retry policy with exponential backoff
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<TaskCanceledException>()
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
    .WaitAndRetryAsync(
        retryCount,
        retryAttempt => TimeSpan.FromSeconds(retryDelaySeconds * Math.Pow(2, retryAttempt - 1)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        });

// Configure HttpClient for Security Check Handler with timeout and retry policy
builder.Services.AddHttpClient<SecurityCheckHandler>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(timeoutSeconds))
    .AddPolicyHandler(retryPolicy);

// Configure HttpClient for Performance Check Handler with timeout and retry policy
builder.Services.AddHttpClient<PerformanceCheckHandler>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(timeoutSeconds))
    .AddPolicyHandler(retryPolicy);

// Configure HttpClient for UI/UX Check Handler with timeout and retry policy
builder.Services.AddHttpClient<UiUxCheckHandler>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(timeoutSeconds))
    .AddPolicyHandler(retryPolicy);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
