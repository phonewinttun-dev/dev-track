using Microsoft.OpenApi;
using DevTrack.Domain;
using DevTrack.Database.Entities;
using DevTrack.Shared.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOptions<AccessTokenOptions>()
    .Bind(builder.Configuration.GetSection(AccessTokenOptions.SectionName))
    .Validate(options => options.ExpiresMinutes > 0, "AccessToken:ExpiresMinutes must be positive.")
    .ValidateOnStart();

builder.Services.AddAuthentication(DevTrack.Api.Auth.TokenCheckHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevTrack.Api.Auth.TokenCheckHandler>(
        DevTrack.Api.Auth.TokenCheckHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<DevTrack.Api.Auth.TokenSetup>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the accessToken from login. Do not include the Bearer prefix."
    });
    options.OperationFilter<DevTrack.Api.Swagger.SwaggerAuth>();
});

builder.Services.AddDbContext<DevTrackDbContext>(options =>
{
    var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";

    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        options.UseInMemoryDatabase(builder.Configuration["DatabaseName"] ?? "DevTrackDb");
    }
    else
    {
        var connection = new SqliteConnectionStringBuilder(
            builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=devtrack.db");

        if (connection.DataSource != ":memory:" && !Path.IsPathRooted(connection.DataSource))
        {
            connection.DataSource = Path.Combine(builder.Environment.ContentRootPath, connection.DataSource);
        }

        options.UseSqlite(connection.ConnectionString);
    }
});

// Feature Services
builder.Services.AddFeatures();

var app = builder.Build();
await app.Services.GetRequiredService<DevTrack.Api.Auth.TokenSetup>().InitializeAsync();
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseSetup>().InitializeAsync();
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
