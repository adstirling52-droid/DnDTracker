using DnDTracker.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace DnDTracker.Web.Services;

public static class AdminRoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services, string adminUsername)
    {
        if (string.IsNullOrWhiteSpace(adminUsername))
        {
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminRoleSeeder");

        if (!await roleManager.RoleExistsAsync(IdentityRoleNames.Admin))
        {
            var createRole = await roleManager.CreateAsync(new IdentityRole(IdentityRoleNames.Admin));
            if (!createRole.Succeeded)
            {
                logger.LogError(
                    "Failed to create {Role} role: {Errors}",
                    IdentityRoleNames.Admin,
                    string.Join(", ", createRole.Errors.Select(error => error.Description)));
                return;
            }
        }

        var adminUser = await userManager.FindByNameAsync(adminUsername.Trim());
        if (adminUser is null)
        {
            logger.LogWarning(
                "Admin user {Username} was not found. Skipping admin role assignment.",
                adminUsername);
            return;
        }

        var usersInAdminRole = await userManager.GetUsersInRoleAsync(IdentityRoleNames.Admin);
        foreach (var user in usersInAdminRole)
        {
            if (user.Id != adminUser.Id)
            {
                var removeResult = await userManager.RemoveFromRoleAsync(user, IdentityRoleNames.Admin);
                if (!removeResult.Succeeded)
                {
                    logger.LogWarning(
                        "Failed to remove {Role} role from user {Username}.",
                        IdentityRoleNames.Admin,
                        user.UserName);
                }
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, IdentityRoleNames.Admin))
        {
            var addResult = await userManager.AddToRoleAsync(adminUser, IdentityRoleNames.Admin);
            if (!addResult.Succeeded)
            {
                logger.LogError(
                    "Failed to assign {Role} role to user {Username}: {Errors}",
                    IdentityRoleNames.Admin,
                    adminUser.UserName,
                    string.Join(", ", addResult.Errors.Select(error => error.Description)));
            }
        }
    }
}
