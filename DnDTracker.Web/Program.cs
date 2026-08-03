using DnDTracker.Web.Components;
using DnDTracker.Web.Components.Account;
using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using DnDTracker.Web.Services;
using DnDTracker.Web.Services.NpcGenerator;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.Configure<SiteSettings>(builder.Configuration.GetSection("SiteSettings"));
builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("SendGrid"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SiteHostService>();

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

builder.Services.AddAuthorization();

builder.Services.AddSingleton<SendGridEmailSender>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<SendGridEmailSender>());

builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<CampaignImportExportService>();
builder.Services.AddScoped<CharacterService>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped<ItemImageService>();
builder.Services.AddScoped<CampaignNpcService>();
builder.Services.AddScoped<CampaignNpcImageService>();
builder.Services.AddScoped<RollTableService>();

builder.Services.AddSingleton<NpcGenerationDataProvider>();
builder.Services.AddScoped<NpcGeneratorService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ItemImageService.MaxFileSizeBytes;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        logger.LogCritical(
            "Connection string 'DefaultConnection' is not configured. " +
            "Set ConnectionStrings__DefaultConnection in IIS application pool environment variables.");
        throw new InvalidOperationException("Database connection string is not configured.");
    }

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<DnDTrackerDbContext>();
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed during startup.");
        throw;
    }

    var contentRoot = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
    Directory.CreateDirectory(Path.Combine(contentRoot, "Data", "item-images"));
    Directory.CreateDirectory(Path.Combine(contentRoot, "Data", "npc-images"));
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/Account/Logout", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    var isTrackerHost = context.Request.Host.Host.StartsWith("tracker.", StringComparison.OrdinalIgnoreCase);
    return Results.LocalRedirect(isTrackerHost ? "~/" : "~/dnd");
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

app.MapGet("/api/campaign-npcs/{npcId:guid}/image", async (
    Guid npcId,
    ClaimsPrincipal user,
    CampaignNpcImageService npcImageService) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var (stream, contentType) = await npcImageService.OpenImageAsync(userId, npcId);
    if (stream is null || contentType is null)
    {
        return Results.NotFound();
    }

    return Results.File(stream, contentType);
}).RequireAuthorization();

app.MapGet("/api/campaigns/{campaignId:guid}/export", async (
    Guid campaignId,
    ClaimsPrincipal user,
    CampaignImportExportService campaignImportExportService) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var (json, fileName, error) = await campaignImportExportService.ExportAsync(userId, campaignId);
    if (error is not null)
    {
        return Results.NotFound();
    }

    return Results.File(
        System.Text.Encoding.UTF8.GetBytes(json!),
        "application/json",
        fileName);
}).RequireAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/send-test-email", async (string to, SendGridEmailSender emailSender) =>
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return Results.BadRequest("Provide a 'to' query parameter with the recipient email address.");
        }

        await emailSender.SendEmailAsync(
            to.Trim(),
            "DnD Tracker SendGrid test",
            "<p>If you received this message, SendGrid is configured correctly in the DnD Tracker app.</p>");

        return Results.Text($"Test email sent to {to.Trim()}.");
    });
}

app.Run();
