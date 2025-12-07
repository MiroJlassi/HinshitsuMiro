using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;
using sib_api_v3_sdk.Client;
using Task = System.Threading.Tasks.Task;

namespace HinshitsuService.Services
{
    public class EmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration config)
        {
            _apiKey = config["Brevo:ApiKey"] ?? throw new ArgumentNullException(nameof(config), "Brevo:ApiKey is required");
            _fromEmail = config["Brevo:FromEmail"] ?? throw new ArgumentNullException(nameof(config), "Brevo:FromEmail is required");
            _fromName = config["Brevo:FromName"] ?? throw new ArgumentNullException(nameof(config), "Brevo:FromName is required");
        }

        public async Task SendVerificationEmailAsync(string toEmail, string code)
        {
            Configuration.Default.ApiKey.Clear();
            Configuration.Default.ApiKey.Add("api-key", _apiKey);

            var apiInstance = new TransactionalEmailsApi();

            var sendSmtpEmailSender = new SendSmtpEmailSender(_fromName, _fromEmail);
            var sendSmtpEmailTo = new SendSmtpEmailTo(toEmail);
            var sendSmtpEmailToList = new List<SendSmtpEmailTo> { sendSmtpEmailTo };

            // need to prepare email templates
            var sendSmtpEmail = new SendSmtpEmail(
                sender: sendSmtpEmailSender,
                to: sendSmtpEmailToList,
                htmlContent: $"<html><body><h1>Verify your email</h1><p>Your verification code is:</p><h2 style='color: #4CAF50; letter-spacing: 5px;'>{code}</h2><p>This code will expire in 15 minutes.</p></body></html>",
                subject: "Your verification code"
            );

            try
            {
                await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error sending email: {e.Message}", e);
            }
        }
    }
}
