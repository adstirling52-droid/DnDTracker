using System.Text;
using DnDTracker.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace DnDTracker.Web.Services;

public class PasswordResetLinkService(
    UserManager<ApplicationUser> userManager,
    IOptions<SiteSettings> siteSettings)
{
    public async Task<PasswordResetLinkResult> GenerateAsync(string usernameOrEmail)
    {
        var lookup = usernameOrEmail.Trim();
        if (string.IsNullOrEmpty(lookup))
        {
            return PasswordResetLinkResult.Error("Enter a username or email address.");
        }

        var user = await userManager.FindByEmailAsync(lookup)
            ?? await userManager.FindByNameAsync(lookup);

        if (user is null)
        {
            return PasswordResetLinkResult.NotFound();
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return PasswordResetLinkResult.Error($"User '{user.UserName}' has no email address on file.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetLink =
            $"{siteSettings.Value.TrackerUrl.TrimEnd('/')}/Account/ResetPassword?userId={user.Id}&code={encodedToken}";

        return PasswordResetLinkResult.Success(user.UserName!, user.Email!, resetLink);
    }
}

public sealed class PasswordResetLinkResult
{
    public bool Succeeded { get; init; }

    public bool UserNotFound { get; init; }

    public string? ErrorMessage { get; init; }

    public string? Username { get; init; }

    public string? Email { get; init; }

    public string? ResetLink { get; init; }

    public static PasswordResetLinkResult Success(string username, string email, string resetLink) =>
        new()
        {
            Succeeded = true,
            Username = username,
            Email = email,
            ResetLink = resetLink
        };

    public static PasswordResetLinkResult NotFound() =>
        new() { UserNotFound = true, ErrorMessage = "No user found with that username or email." };

    public static PasswordResetLinkResult Error(string message) =>
        new() { ErrorMessage = message };
}
