using System.Net;
using System.Net.Mail;


namespace Saas.Api.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendInviteEmailAsync(string targetEmail, string role, string tenantId, string inviteToken)
        {
            // 1. Build the single-use registration link pointing to our Next.js frontend
            string registrationLink = $"http://localhost:3000/register?token={inviteToken}&tenantId={tenantId}";

            // 2. Draft the professional HTML layout
            string emailBody = $@"
            <div style='font-family: sans-serif; background-color: #020617; color: #ffffff; padding: 40px; border-radius: 12px; max-width: 600px;'>
                <h2 style='color: #818cf8; margin-bottom: 4px;'>vanguard.io</h2>
                <p style='color: #94a3b8; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 24px;'>System Access Initialization Notice</p>
                <p>Hello,</p>
                <p>An administrator has provisioned an authorized account identity for you within your organization's isolated workspace node.</p>
                <div style='background-color: #0f172a; border: 1px solid #1e293b; padding: 16px; border-radius: 8px; margin: 24px 0;'>
                    <p style='margin: 0; font-size: 14px; color: #94a3b8;'><strong>Workspace Clearance:</strong> {role}</p>
                    <p style='margin: 4px 0 0 0; font-size: 14px; color: #94a3b8;'><strong>Associated Identifier:</strong> {targetEmail}</p>
                </div>
                <p style='margin-bottom: 32px;'>To claim your identity credentials and initialize your workspace session, please click the secure authorization link below:</p>
                <a href='{registrationLink}' style='background-color: #4f46e5; color: #ffffff; padding: 12px 24px; text-decoration: none; font-weight: bold; border-radius: 6px; display: inline-block;'>Initialize Secure Workspace Session</a>
                <hr style='border: 0; border-top: 1px solid #1e293b; margin: 32px 0;' />
                <p style='font-size: 11px; color: #64748b;'>Security Warning: This access token is encrypted, single-use, and expires in 24 hours.</p>
            </div>";

            // 3. Configure the SMTP client messenger settings
            using var message = new MailMessage();
            message.To.Add(new MailAddress(targetEmail));
            message.From = new MailAddress("no-reply@vanguard.com", "Vanguard Security Core");
            message.Subject = "[Action Required] Provisioning Workspace Node Invitation";
            message.Body = emailBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient("sandbox.smtp.mailtrap.io", 2525) // Change to your real provider in production
            {
                Credentials = new NetworkCredential("e5a6a62abc9271", "228386533f9379"),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}
