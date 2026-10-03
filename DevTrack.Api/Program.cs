using DevTrack.Domain.Security;
using DevTrack.Database;
using DevTrack.Database.Entities;
using DevTrack.Domain.Features.Batches;
using DevTrack.Domain.Features.Developers;
using DevTrack.Domain.Features.Training;
using DevTrack.Domain.Features.Dashboard;
using DevTrack.Shared.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => options.IsValid(), "Jwt must include Issuer, Audience, a SigningKey of at least 32 characters, and a positive ExpiresMinutes value.")
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.ConfigureOptions<DevTrack.Api.Auth.ConfigureJwtBearerOptions>();

builder.Services.AddAuthorization();
builder.Services.AddScoped<RbacService>();

builder.Services.AddScoped<DevTrack.Api.Auth.JwtTokenService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

builder.Services.AddDbContext<DevTrackDbContext>(options =>
{
    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        options.UseInMemoryDatabase(builder.Configuration["DatabaseName"] ?? "DevTrackDb");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// Feature Services
builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<IDeveloperService, DeveloperService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase)) db.Database.EnsureCreated();
    await scope.ServiceProvider.GetRequiredService<RbacService>().SeedAsync(builder.Configuration["AdminAuth:Username"], builder.Configuration["AdminAuth:Password"], builder.Configuration["AdminAuth:DisplayName"] ?? "System Admin");
}


// Ensure Database is Created (for In-Memory)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        context.Database.EnsureCreated();
        DbInitializer.Initialize(context);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();
