using System.Text.RegularExpressions;
using DnDTracker.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DnDTracker.Web.Services;

public partial class SendGridEmailSender(
    IOptions<SendGridSettings> options,
    ILogger<SendGridEmailSender> logger) : IEmailSender<ApplicationUser>
{
    private readonly SendGridSettings _settings = options.Value;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendEmailAsync(
            email,
            "Confirm your DnD Tracker email",
            $"""
            <p>Please confirm your account by <a href="{confirmationLink}">clicking here</a>.</p>
            """);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendEmailAsync(
            email,
            "Reset your DnD Tracker password",
            $"""
            <p>Please reset your password by <a href="{resetLink}">clicking here</a>.</p>
            <p>If you did not request this, you can ignore this email.</p>
            """);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendEmailAsync(
            email,
            "Reset your DnD Tracker password",
            $"""
            <p>Your password reset code is:</p>
            <p><strong>{resetCode}</strong></p>
            <p>If you did not request this, you can ignore this email.</p>
            """);

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            logger.LogWarning(
                "SendGrid API key is not configured. Email to {Email} with subject {Subject} was not sent.",
                email,
                subject);
            throw new InvalidOperationException("Email is not configured.");
        }

        var client = new SendGridClient(_settings.ApiKey);
        var message = MailHelper.CreateSingleEmail(
            new EmailAddress(_settings.FromEmail, _settings.FromName),
            new EmailAddress(email),
            subject,
            plainTextContent: HtmlToPlainText(htmlMessage),
            htmlContent: htmlMessage);

        var response = await client.SendEmailAsync(message);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("SendGrid accepted email to {Email} with subject {Subject}.", email, subject);
            return;
        }

        var body = await response.Body.ReadAsStringAsync();
        logger.LogError(
            "SendGrid returned {StatusCode} when sending email to {Email}: {Body}",
            response.StatusCode,
            email,
            body);

        throw new InvalidOperationException("Failed to send email.");
    }

    private static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return HtmlTagRegex().Replace(html, string.Empty).Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
