using HinshitsuSecurity.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HinshitsuSecurity.Services
{
    public class SecurityHeadersAnalyzer : ISecurityHeadersAnalyzer
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SecurityHeadersAnalyzer> _logger;

        // Critical security headers to analyze (can be exported for more controle in the future) 
        private readonly string[] _criticalHeaders = new[]
        {
            "Strict-Transport-Security",
            "Content-Security-Policy",
            "X-Frame-Options",
            "X-Content-Type-Options",
            "Referrer-Policy",
            "Permissions-Policy",
            "Cross-Origin-Embedder-Policy",
            "Cross-Origin-Opener-Policy",
            "Cross-Origin-Resource-Policy"
        };

        public SecurityHeadersAnalyzer(HttpClient httpClient, ILogger<SecurityHeadersAnalyzer> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 HinshitsuSecurityAnalyzer/1.0");
        }

        public async Task<string> AnalyzeSecurityHeadersAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new ArgumentException("Invalid URL format", nameof(url));

            try
            {
                _logger.LogInformation("Analyzing security headers for URL: {Url}", url);

                // First try HEAD request (faster)
                HttpResponseMessage? response = null;
                try
                {
                    response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
                }
                catch
                {
                    // If HEAD fails, try GET
                    _logger.LogInformation("HEAD request failed, trying GET for URL: {Url}", url);
                    response = await _httpClient.GetAsync(uri);
                }

                var headers = new Dictionary<string, object>();
                foreach (var headerName in _criticalHeaders)
                {
                    if (response.Headers.TryGetValues(headerName, out var values))
                    {
                        headers[headerName] = new
                        {
                            present = true,
                            value = string.Join(", ", values),
                            status = "configured"
                        };
                    }
                    else if (response.Content.Headers.TryGetValues(headerName, out var contentValues))
                    {
                        headers[headerName] = new
                        {
                            present = true,
                            value = string.Join(", ", contentValues),
                            status = "configured"
                        };
                    }
                    else
                    {
                        headers[headerName] = new
                        {
                            present = false,
                            value = (string?)null,
                            status = "missing"
                        };
                    }
                }

                // Score calculation
                var presentCount = headers.Count(h => ((dynamic)h.Value).present);
                var totalCount = _criticalHeaders.Length;
                var score = (double)presentCount / totalCount * 100;
                var grade = CalculateGrade(score);
                var severity = CalculateSeverity(score);

                var result = new
                {
                    url,
                    statusCode = (int)response.StatusCode,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    headers,
                    summary = new
                    {
                        totalHeaders = totalCount,
                        presentHeaders = presentCount,
                        missingHeaders = totalCount - presentCount,
                        score = Math.Round(score, 2),
                        grade,
                        severity
                    }
                };

                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Successfully analyzed security headers for URL: {Url}. Grade: {Grade}", url, grade);

                return json;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for URL: {Url}", url);
                throw new InvalidOperationException($"Failed to connect to {url}. Please verify the URL is accessible.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout for URL: {Url}", url);
                throw new TimeoutException($"Request to {url} timed out after 30 seconds.", ex);
            }
        }

        private static string CalculateGrade(double score)
        {
            return score switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
        }

        private static string CalculateSeverity(double score)
        {
            return score switch
            {
                >= 90 => "low",
                >= 70 => "medium",
                >= 50 => "high",
                _ => "critical"
            };
        }
    }
}
