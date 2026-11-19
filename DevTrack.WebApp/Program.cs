using DevTrack.WebApp.Services;
using DevTrack.WebApp.Auth;
using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Features.TrainingManagement.Developers;
using DevTrack.Features.TrainingManagement.Training;
using DevTrack.Features.TrainingManagement.Dashboard;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
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

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthApiClient>();
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

var app = builder.Build();


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
