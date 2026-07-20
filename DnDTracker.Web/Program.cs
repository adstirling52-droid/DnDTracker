using DnDTracker.Web.Components;
using DnDTracker.Web.Components.Account;
using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using DnDTracker.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DnDTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<DnDTrackerDbContext>()
    .AddDefaultTokenProviders();

builder.Services.RemoveAll<IUserValidator<ApplicationUser>>();
builder.Services.AddScoped<IUserValidator<ApplicationUser>, OptionalEmailUserValidator>();

builder.Services.AddAuthorization();

builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<CharacterService>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped<ItemImageService>();
builder.Services.AddScoped<RollTableService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ItemImageService.MaxFileSizeBytes;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/Account/Logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("~/");
});

app.MapGet("/api/items/{itemId:guid}/image", async (
    Guid itemId,
    ClaimsPrincipal user,
    ItemImageService itemImageService) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var (stream, contentType) = await itemImageService.OpenImageAsync(userId, itemId);
    if (stream is null || contentType is null)
    {
        return Results.NotFound();
    }

    return Results.File(stream, contentType);
}).RequireAuthorization();

app.Run();
