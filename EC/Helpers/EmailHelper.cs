using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EC.Helpers;

public class EmailHelper
{
    private readonly IConfiguration _config;

    public EmailHelper(IConfiguration config)
    {
        _config = config;
    }

    private string _host { get { return _config["SmtpSettings:Host"]; } }
    private int _port { get { return int.Parse(_config["SmtpSettings:Port"]); } }
    private string _user { get { return _config["SmtpSettings:UserName"]; } }
    private string _pass { get { return _config["SmtpSettings:Password"]; } }
    private bool _enableSsl { get { return bool.Parse(_config["SmtpSettings:EnableSsl"]); } }
    private bool _isTest { get { return bool.Parse(_config["SmtpSettings:IsTest"]); } }
    private string _testToEmail { get { return _config["SmtpSettings:TestToEmail"]; } }
    private string _fromEmail { get { return _config["SmtpSettings:FromEmail"]; } }
    private string _fromName { get { return _config["SmtpSettings:FromName"]; } }

    // ================= RESET EMAIL =================
    public async Task SendResetEmail(string toEmail, string link)
    {
        var Subject = "Password Reset";
        var Body = $"Click link to reset password:\n\n{link}";

        await SendEmail(subject: Subject, body: Body, [toEmail]);
    }

    // ================= INVOICE EMAIL =================
    public async Task SendInvoiceEmail(string toEmail, string userName, int orderId, decimal totalAmount)
    {
        var Subject = $"Invoice - Order #{orderId}";
        var Body = $@"
                <h2>Invoice</h2>
                <p>Hello {userName},</p>
                <p>Thank you for your order.</p>
                <p><b>Order ID:</b> {orderId}</p>
                <p><b>Total Amount:</b> ₹{totalAmount}</p>
                <br/>
                <p>Regards,<br/>EC Store</p>
            ";

        await SendEmail(subject: Subject, body: Body, [toEmail]);
    }

    public async Task SendEmail(string subject, string body, List<string> toEmails)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_fromName, _fromEmail));

        if (_isTest)
        {
            toEmails = [_testToEmail];
        }

        foreach (var toEmail in toEmails)
        {
            email.To.Add(MailboxAddress.Parse(toEmail));
        }

        email.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body // Set the email body (can be plain text or HTML)
        };

        // Add the attachment
        //if (!string.IsNullOrEmpty(attachmentFilePath) && File.Exists(attachmentFilePath))
        //{
        //    byte[] fileBytes = File.ReadAllBytes(attachmentFilePath);
        //    // Use Path.GetFileName to get just the filename from the path
        //    builder.Attachments.Add(Path.GetFileName(attachmentFilePath), fileBytes, ContentType.Parse("application/octet-stream"));
        //}

        email.Body = builder.ToMessageBody();

        using (var smtp = new SmtpClient())
        {
            var socketOption = _enableSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

            // Connect to the SMTP server
            await smtp.ConnectAsync(_host, _port, socketOption);

            // Authenticate with credentials
            await smtp.AuthenticateAsync(_user, _pass);

            // Send the email
            await smtp.SendAsync(email);

            // Disconnect from the server
            await smtp.DisconnectAsync(true);
        }
    }
}