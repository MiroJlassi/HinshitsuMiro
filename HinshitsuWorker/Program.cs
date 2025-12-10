using HinshitsuWorker;
using HinshitsuWorker.Services;
using Polly;
using Polly.Extensions.Http;

Console.WriteLine("========================================");
Console.WriteLine($"HinshitsuWorker Starting... {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine("========================================");

var builder = Host.CreateApplicationBuilder(args);

Console.WriteLine("Builder created. Configuring services...");

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetryWorkerService();
Console.WriteLine("Application Insights configured.");

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

Console.WriteLine("All services configured. Building host...");

var host = builder.Build();

Console.WriteLine("Host built. Starting worker service...");

await host.RunAsync();

Console.WriteLine("Worker service stopped.");
