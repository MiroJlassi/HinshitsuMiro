using Microsoft.Extensions.Logging;

namespace HinshitsuSecurity.Services
{
    /// <summary>
    /// Generates structured prompts for security analysis
    /// </summary>
    public class PromptGenerator
    {
        private readonly ILogger<PromptGenerator>? _logger;

        public PromptGenerator(ILogger<PromptGenerator>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generates the system prompt for the security auditor role
        /// </summary>
        public string GetSystemPrompt()
        {
            return "You are a cybersecurity expert. Analyze security headers and provide clear, concise recommendations.";
        }

        /// <summary>
        /// Generates the user prompt for security headers analysis with structured output
        /// </summary>
        /// <param name="url">The URL being analyzed</param>
        /// <param name="securityReport">The JSON security report from the headers analyzer</param>
        public string GetSecurityAnalysisPrompt(string url, string securityReport)
        {
            _logger?.LogDebug("Generating security analysis prompt for URL: {Url}", url);

            return $@"Analyze security headers for: {url}

Report:
{securityReport}

Return your analysis as a valid JSON object with this exact structure:
{{
  ""summary"": {{
    ""grade"": ""A-F grade"",
    ""riskLevel"": ""Critical/High/Medium/Low"",
    ""configuredHeaders"": number,
    ""missingHeaders"": number,
    ""overallAssessment"": ""brief assessment""
  }},
  ""issuesAndRisks"": [
    {{
      ""header"": ""header name"",
      ""issue"": ""what's wrong"",
      ""risk"": ""security risk"",
      ""attackVectors"": [""attack type 1"", ""attack type 2""]
    }}
  ],
  ""suggestions"": [
    {{
      ""priority"": 1,
      ""header"": ""header name"",
      ""description"": ""what to do"",
      ""implementation"": ""exact header value""
    }}
  ]
}}

Focus on the most critical issues. Keep it concise.";
        }
    }
}
