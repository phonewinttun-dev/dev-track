using DevTrack.Database.Entities;
using Microsoft.EntityFrameworkCore;
using DevTrack.Domain.Security;
using DevTrack.WebApp.Services;
using DevTrack.WebApp.Auth;
using DevTrack.Domain.Features.Batches;
using DevTrack.Domain.Features.Developers;
using DevTrack.Domain.Features.Training;
using DevTrack.Domain.Features.Dashboard;
using DevTrack.Shared.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/webapp-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => options.IsValid(), "Jwt must include Issuer, Audience, a SigningKey of at least 32 characters, and a positive ExpiresMinutes value.")
    .ValidateOnStart();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<RbacService>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAdminJwtTokenService, AdminJwtTokenService>();
builder.Services.AddTransient<AdminJwtAuthorizationHandler>();

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7267";

// Register HTTP API clients
builder.Services.AddHttpClient(ApiClientBase.ClientName, client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AdminJwtAuthorizationHandler>();

builder.Services.AddScoped<IBatchApiClient, BatchApiClient>();
builder.Services.AddScoped<IDeveloperApiClient, DeveloperApiClient>();
builder.Services.AddScoped<ITrainingApiClient, TrainingApiClient>();
builder.Services.AddScoped<IDashboardApiClient, DashboardApiClient>();

// Feature Services (Api Versions)
builder.Services.AddScoped<IBatchService, BatchApiService>();
builder.Services.AddScoped<IDeveloperService, DeveloperApiService>();
builder.Services.AddScoped<ITrainingService, TrainingApiService>();
builder.Services.AddScoped<IDashboardService, DashboardApiService>();

var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
builder.Services.AddDbContext<DevTrackDbContext>(options =>
{
    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase)) options.UseInMemoryDatabase(builder.Configuration["DatabaseName"] ?? "DevTrackRbac");
    else options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase)) db.Database.EnsureCreated();
    await scope.ServiceProvider.GetRequiredService<RbacService>().SeedAsync(builder.Configuration["AdminAuth:Username"], builder.Configuration["AdminAuth:Password"], builder.Configuration["AdminAuth:DisplayName"] ?? "System Admin");
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

Log.CloseAndFlush();
