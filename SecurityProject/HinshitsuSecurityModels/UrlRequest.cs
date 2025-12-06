using System.ComponentModel.DataAnnotations;

namespace HinshitsuSecurity.Models
{
    /// <summary>
    /// Request model for security audit
    /// </summary>
    public class UrlRequest
    {
        /// <summary>
        /// The website URL to analyze
        /// </summary>
        [Required(ErrorMessage = "URL is required")]
        [Url(ErrorMessage = "Please provide a valid URL")]
        [MaxLength(2048, ErrorMessage = "URL must not exceed 2048 characters")]
        public string Url { get; set; } = string.Empty;
    }
}
