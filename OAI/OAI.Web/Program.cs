using System.Globalization;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using OAI.Application;
using OAI.Application.Abstractions.Services;
using OAI.Infrastructure;
using OAI.Infrastructure.Hangfire;
using OAI.Infrastructure.Identity;
using OAI.Infrastructure.Persistence;
using OAI.Web.Components;
using OAI.Web.Endpoints;
using OAI.Web.Localization;
using OAI.Web.Options;
using OAI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOaiHangfireStorage(builder.Configuration);

builder.Services.AddOptions<ApplicationInfoOptions>()
    .Bind(builder.Configuration.GetRequiredSection("ApplicationInfo"));

builder.Services.Configure<IdentitySeedOptions>(
    builder.Configuration.GetSection("IdentitySeed"));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<OaiDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.LogoutPath = "/auth/logout";
    options.ReturnUrlParameter = "returnUrl";
});

builder.Services.AddAuthorization(options =>
{
    options.AddOaiPolicies();
});
builder.Services.AddScoped<IdentityDataSeeder>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserTimeZoneService>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<CurrentUserAuthorizationService>();
builder.Services.AddScoped<LocalizedMessageResolver>();
builder.Services.AddScoped<IToastService, ToastService>();

var app = builder.Build();

var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("vi")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

localizationOptions.RequestCultureProviders.Insert(
    0,
    new CookieRequestCultureProvider()
);

app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    DashboardTitle = "OAI Background Jobs",
    Authorization =
    [
        new HangfireDashboardAuthorizationFilter()
    ]
});

app.MapStaticAssets();

app.MapLocalizationEndpoints();
app.MapAuthEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapDemoDataEndpoints();
    app.MapSystemHealthEndpoints();
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
    await identitySeeder.SeedAsync();
}

app.Run();
