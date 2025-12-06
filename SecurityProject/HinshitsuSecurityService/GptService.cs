using Azure;
using Azure.AI.OpenAI;
using HinshitsuSecurity.Services.Interfaces;
using HinshitsuSecurity.Models;
using HinshitsuSecurity.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace HinshitsuSecurity.Services
{
    public class GptService : IGptService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<GptService> _logger;
        private readonly PromptGenerator _promptGenerator;

        public GptService(IOptions<AzureOpenAIOptions> options, ILogger<GptService> logger)
        {
            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _promptGenerator = new PromptGenerator();

            var endpoint = new Uri(opts.Endpoint);
            var credential = new AzureKeyCredential(opts.ApiKey);

            var azureClient = new AzureOpenAIClient(endpoint, credential);
            _chatClient = azureClient.GetChatClient(opts.Deployment);
        }

        // Miro here : in order to add more controle on the GPT response better have a structured output

        //public async Task<string> AnalyzeAsync(string securityJson, string url)
        //{
        //    if (string.IsNullOrWhiteSpace(securityJson))
        //        throw new ArgumentException("Security JSON cannot be null or empty", nameof(securityJson));

        //    if (string.IsNullOrWhiteSpace(url))
        //        throw new ArgumentException("URL cannot be null or empty", nameof(url));

        //    try
        //    {
        //        _logger.LogInformation("Starting AI analysis for URL: {Url}", url);

        //        var messages = new List<ChatMessage>
        //        {
        //            new SystemChatMessage(_promptGenerator.GetSystemPrompt()),
        //            new UserChatMessage(_promptGenerator.GetSecurityAnalysisPrompt(url, securityJson))
        //        };

        //        var response = await _chatClient.CompleteChatAsync(messages);
        //        var analysisText = response.Value.Content[0].Text;

        //        _logger.LogInformation("Successfully completed AI analysis for URL: {Url}", url);
        //        return analysisText;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error during AI analysis for URL: {Url}", url);
        //        throw;
        //    }
        //}

        public async Task<(SecurityAnalysis? Structured, string Raw)> AnalyzeStructuredAsync(string securityJson, string url)
        {
            if (string.IsNullOrWhiteSpace(securityJson))
                throw new ArgumentException("Security JSON cannot be null or empty", nameof(securityJson));

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            try
            {
                _logger.LogInformation("Starting structured AI analysis for URL: {Url}", url);

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(_promptGenerator.GetSystemPrompt()),
                    new UserChatMessage(_promptGenerator.GetSecurityAnalysisPrompt(url, securityJson))
                };

                var response = await _chatClient.CompleteChatAsync(messages);
                var analysisText = response.Value.Content[0].Text;

                SecurityAnalysis? structuredAnalysis = null;
                try
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    structuredAnalysis = JsonSerializer.Deserialize<SecurityAnalysis>(analysisText, jsonOptions);
                    _logger.LogInformation("Successfully parsed structured AI analysis for URL: {Url}", url);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogWarning(jsonEx, "Failed to parse JSON response from GPT for URL: {Url}. Returning raw text.", url);
                }

                return (structuredAnalysis, analysisText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during structured AI analysis for URL: {Url}", url);
                throw;
            }
        }
    }
}
