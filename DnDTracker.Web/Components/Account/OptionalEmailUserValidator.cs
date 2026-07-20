using DnDTracker.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace DnDTracker.Web.Components.Account;

public class OptionalEmailUserValidator : UserValidator<ApplicationUser>
{
    public OptionalEmailUserValidator(IdentityErrorDescriber? errors = null)
        : base(errors)
    {
    }

    public override async Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            user.Email = null;
            user.NormalizedEmail = null;
        }

        var result = await base.ValidateAsync(manager, user);

        if (result.Succeeded || !string.IsNullOrWhiteSpace(user.Email))
        {
            return result;
        }

        var errors = result.Errors
            .Where(error => error.Code != "InvalidEmail")
            .ToArray();

        return errors.Length > 0
            ? IdentityResult.Failed(errors)
            : IdentityResult.Success;
    }
}
