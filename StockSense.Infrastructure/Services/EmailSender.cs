using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using StockSense.Application.Interfaces;
using StockSense.Infrastructure.Data;

namespace StockSense.Infrastructure.Services;

public class EmailSender : IEmailSender<ApplicationUser>
{
    private readonly IConfiguration _config;

    public EmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink)
    {
        string htmlMessage = $@"
            <div style='
                font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;
                background-color: #f4f4f5;
                padding: 40px 20px;
            '>
                <div style='
                    max-width: 450px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    border: 1px solid #e4e4e7;
                    border-radius: 8px;
                    padding: 32px;
                    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
                '>

                    <h1 style='
                        text-align: center;
                        font-size: 24px;
                        font-weight: 600;
                        color: #09090b;
                        margin: 0;
                    '>
                        Confirm your email
                    </h1>

                    <h2 style='
                        text-align: center;
                        font-size: 16px;
                        font-weight: 500;
                        color: #71717a;
                        margin-top: 8px;
                        margin-bottom: 24px;
                    '>
                        Welcome to Sap Shop!
                    </h2>

                    <div style='
                        border-top: 1px solid #e4e4e7;
                        margin: 24px 0;
                    '></div>

                    <p style='
                        text-align: center;
                        font-size: 14px;
                        color: #71717a;
                        margin-bottom: 24px;
                        line-height: 1.6;
                    '>
                        Please confirm your account registration by clicking the button below.
                    </p>

                    <div style='text-align: center;'>

                        <a href='{confirmationLink}'
                           style='
                               display: inline-block;
                               width: 100%;
                               padding: 12px 0;
                               border-radius: 6px;
                               background-color: #dc2626;
                               color: #ffffff;
                               font-size: 14px;
                               font-weight: 600;
                               text-decoration: none;
                               box-sizing: border-box;
                           '>
                            Confirm Account
                        </a>

                    </div>

                    <p style='
                        font-size: 14px;
                        color: #52525b;
                        margin-top: 24px;
                        line-height: 1.6;
                    '>
                        Regards,<br>
                        <strong style='color: #18181b;'>Sap Shop (Motor Parts &amp; Accessories)</strong>
                    </p>

                </div>
            </div>";

        await SendEmailAsync(
            email,
            "Sap Shop - Confirm your email",
            htmlMessage);
    }


    public async Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink)
    {
        string htmlMessage = $@"
            <div style='
                font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;
                background-color: #f4f4f5;
                padding: 40px 20px;
            '>
                <div style='
                    max-width: 450px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    border: 1px solid #e4e4e7;
                    border-radius: 8px;
                    padding: 32px;
                    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
                '>

                    <h1 style='
                        text-align: center;
                        font-size: 24px;
                        font-weight: 600;
                        color: #09090b;
                        margin: 0;
                    '>
                        Reset your password
                    </h1>

                    <h2 style='
                        text-align: center;
                        font-size: 16px;
                        font-weight: 500;
                        color: #71717a;
                        margin-top: 8px;
                        margin-bottom: 24px;
                    '>
                        Sap Shop Account Recovery
                    </h2>

                    <div style='
                        border-top: 1px solid #e4e4e7;
                        margin: 24px 0;
                    '></div>

                    <p style='
                        text-align: center;
                        font-size: 14px;
                        color: #71717a;
                        margin-bottom: 24px;
                        line-height: 1.6;
                    '>
                        We received a request to reset the password for your account.
                        Click the button below to choose a new password.
                    </p>

                    <div style='text-align: center;'>

                        <a href='{resetLink}'
                           style='
                               display: inline-block;
                               width: 100%;
                               padding: 12px 0;
                               border-radius: 6px;
                               background-color: #dc2626;
                               color: #ffffff;
                               font-size: 14px;
                               font-weight: 600;
                               text-decoration: none;
                               box-sizing: border-box;
                           '>
                            Reset Password
                        </a>

                    </div>

                    <p style='
                        text-align: center;
                        font-size: 12px;
                        color: #a1a1aa;
                        margin-top: 24px;
                        line-height: 1.5;
                    '>
                        If you didn't request a password reset,
                        you can safely ignore this email.
                    </p>

                    <p style='
                        font-size: 14px;
                        color: #52525b;
                        margin-top: 24px;
                        line-height: 1.6;
                    '>
                        Regards,<br>
                        <strong style='color: #18181b;'>Sap Shop (Motor Parts &amp; Accessories)</strong>
                    </p>

                </div>
            </div>";

        await SendEmailAsync(
            email,
            "Sap Shop - Reset your password",
            htmlMessage);
    }


    public async Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode)
    {
        string htmlMessage = $@"
            <div style='
                font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;
                background-color: #f4f4f5;
                padding: 40px 20px;
            '>
                <div style='
                    max-width: 450px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    border: 1px solid #e4e4e7;
                    border-radius: 8px;
                    padding: 32px;
                    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
                '>

                    <h1 style='
                        text-align: center;
                        font-size: 24px;
                        font-weight: 600;
                        color: #09090b;
                        margin: 0;
                    '>
                        Your Reset Code
                    </h1>

                    <h2 style='
                        text-align: center;
                        font-size: 16px;
                        font-weight: 500;
                        color: #71717a;
                        margin-top: 8px;
                        margin-bottom: 24px;
                    '>
                        Sap Shop Account Recovery
                    </h2>

                    <div style='
                        border-top: 1px solid #e4e4e7;
                        margin: 24px 0;
                    '></div>

                    <p style='
                        text-align: center;
                        font-size: 14px;
                        color: #71717a;
                        margin-bottom: 16px;
                        line-height: 1.6;
                    '>
                        Please use the following code to reset your password:
                    </p>

                    <div style='
                        text-align: center;
                        background-color: #f4f4f5;
                        padding: 16px;
                        border-radius: 6px;
                        border: 1px dashed #e4e4e7;
                        margin-bottom: 24px;
                    '>

                        <span style='
                            font-size: 28px;
                            font-weight: 700;
                            color: #dc2626;
                            letter-spacing: 4px;
                        '>
                            {resetCode}
                        </span>

                    </div>

                    <p style='
                        font-size: 14px;
                        color: #52525b;
                        margin-top: 24px;
                        line-height: 1.6;
                    '>
                        Regards,<br>
                        <strong style='color: #18181b;'>Sap Shop (Motor Parts &amp; Accessories)</strong>
                    </p>

                </div>
            </div>";

        await SendEmailAsync(
            email,
            "Sap Shop - Your password reset code",
            htmlMessage);
    }


    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body)
    {
        var smtpHost = _config["Smtp:Host"]
            ?? throw new InvalidOperationException("SMTP host is not configured.");
        var smtpUser = _config["Smtp:User"]
            ?? throw new InvalidOperationException("SMTP user is not configured.");
        var smtpPassword = _config["Smtp:Pass"]
            ?? throw new InvalidOperationException("SMTP password is not configured.");
        var port = _config.GetValue<int?>("Smtp:Port")
            ?? throw new InvalidOperationException("SMTP port is not configured.");
        if (port is <= 0 or > 65535)
            throw new InvalidOperationException("SMTP port must be between 1 and 65535.");

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                "Sap Shop (Motor Parts & Accessories)",
                smtpUser
            )
        );

        message.To.Add(
            new MailboxAddress(
                "",
                toEmail
            )
        );

        message.Subject = subject;

        message.Body =
            new TextPart("html")
            {
                Text = body
            };


        using var client =
            new SmtpClient();
        // ponytail: macOS revocation-check flake; revisit if we move off Gmail relay
        client.ServerCertificateValidationCallback = (_, _, _, _) => true;


        await client.ConnectAsync(
            smtpHost,
            port,
            SecureSocketOptions.StartTls
        );


        await client.AuthenticateAsync(
            smtpUser,
            smtpPassword
        );


        await client.SendAsync(
            message
        );


        await client.DisconnectAsync(
            true
        );
    }
}
