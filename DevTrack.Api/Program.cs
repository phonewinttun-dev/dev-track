using DevTrack.Features.Access.Auth;
using DevTrack.Domain;
using DevTrack.Domain.Access;
using DevTrack.Database.Entities;
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
builder.Services.AddAccessModule();

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
builder.Services.AddTrainingManagementModule();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
    var isInMemory = db.Database.IsInMemory();
    if (isInMemory)
    {
        await db.Database.EnsureCreatedAsync();
    }

    await scope.ServiceProvider.GetRequiredService<RbacService>().SeedAsync(builder.Configuration["AdminAuth:Username"], builder.Configuration["AdminAuth:Password"], builder.Configuration["AdminAuth:DisplayName"] ?? "System Admin");
    if (isInMemory)
    {
        await DbInitializer.InitializeAsync(db);
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
