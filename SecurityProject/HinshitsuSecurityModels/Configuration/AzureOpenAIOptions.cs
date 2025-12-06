using System.ComponentModel.DataAnnotations;

namespace HinshitsuSecurity.Models.Configuration
{
    /// <summary>
    /// Configuration options for Azure OpenAI service integration
    /// </summary>
    public class AzureOpenAIOptions
    {
        public const string SectionName = "AzureOpenAI";

        /// <summary>
        /// Azure OpenAI API key
        /// </summary>
        [Required(ErrorMessage = "Azure OpenAI API key is required")]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Azure OpenAI endpoint URL
        /// </summary>
        [Required(ErrorMessage = "Azure OpenAI endpoint is required")]
        [Url(ErrorMessage = "Endpoint must be a valid URL")]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Deployment name for the model
        /// </summary>
        [Required(ErrorMessage = "Deployment name is required")]
        public string Deployment { get; set; } = string.Empty;

        /// <summary>
        /// API version to use
        /// </summary>
        public string ApiVersion { get; set; } = "2024-06-01";

        /// <summary>
        /// Timeout for API requests in seconds
        /// </summary>
        [Range(10, 300)]
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Temperature setting for GPT responses (0.0 - 2.0)
        /// </summary>
        [Range(0.0, 2.0)]
        public double Temperature { get; set; } = 0.2;
    }
}
