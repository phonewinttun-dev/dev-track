# DevTrack Management System

DevTrack is a comprehensive management system designed to track training batches, developers, and attendance. It is built using a modern, decoupled architecture with .NET 10, following the **Result Pattern** and leveraging **IHttpClientFactory** for API-driven communication.

## Project Structure

DevTrack has one backend with two domain modules and a separate ASP.NET Core MVC frontend.

- **DevTrack.Api** is the backend composition root. It registers the modules explicitly, exposes thin controllers, and owns database configuration and startup seeding.
- **DevTrack.Domain** is the Training Management module. Its feature folders are `Dashboard`, `Batches`, `Developers`, and `Training`. The Attendance menu maps to `Training`, which also owns the schedule and reports.
- **DevTrack.Domain.Access** owns the `Auth` feature, including credentials, roles, permissions, and bootstrap.
- **DevTrack.Models** holds typed requests, responses, and feature service contracts. Its `Features/<Module>/<Feature>` folders mirror the domain features and share their namespaces.
- **DevTrack.Database** owns the EF Core context and persistence mappings used by the domain modules.
- **DevTrack.Shared** holds the result types, pagination, and shared constants.
- **DevTrack.WebApp** references Models and reaches the API over HTTP for feature data, login, and live permissions. It owns the MVC views and browser cookie.

### Adding a feature

Place business logic in the owning module's `Features/<Feature>` folder and shared DTOs in its matching Models folder. Register the service in that module's DI extension and expose it through a thin API controller. Keep secondary actions such as schedule and export in the menu's feature. If one domain module needs another, put the interface and DTO in Models and register its implementation in the API.

## Features

- **Batch Management**: Create training batches, define mentors, and manage training durations.
- **Calendar Auto-Generation**: Automatically generates a training calendar based on specified start/end dates and training days.
- **Developer Tracking**: Manage developer profiles and assign them to specific batches.
- **Attendance System**: 
  - Bulk attendance marking for training dates.
  - Eligibility tracking based on attendance percentages.
  - Real-time summary views for mentors and administrators.

## Getting Started

### Prerequisites
- .NET 10 SDK (`10.0.404` or a compatible newer feature band)
- SQL Server

### Configuration
1. **Database**: Configure the SQL Server connection for the API and apply `init_db.sql` and `rbac_schema.sql` (see Dynamic RBAC below). For an existing database, rerun `init_db.sql` to add the attendance uniqueness index. Reconcile any duplicate marks for the same batch, developer, and date before applying the index.
2. **API URL**: Update the `ApiSettings:BaseUrl` in `DevTrack.WebApp/appsettings.json` to match the API's hosting URL.
3. **Admin login**: Configure `AdminAuth:Username`, `AdminAuth:Password`, and `AdminAuth:DisplayName` for the API through user secrets or environment variables to bootstrap the first database user.
4. **JWT**: Configure `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:SigningKey` in `DevTrack.Api`. Use a signing key with at least 32 characters and keep production keys out of committed appsettings files.

### Running the Application
1. Start the **DevTrack.Api** project.
2. Start the **DevTrack.WebApp** project.
3. Access the system via the WebApp URL.

### Local learning setup

The API defaults to an in-memory database. Development launch profiles load API user secrets. From the repository root, run this once in PowerShell to create a local `learner` account and a random signing key. Save the password printed by the script for MVC login.

```powershell
$learningPassword = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(16))
$learningKey = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
@{
    'AdminAuth:Username' = 'learner'
    'AdminAuth:Password' = $learningPassword
    'Jwt:SigningKey' = $learningKey
} | ConvertTo-Json | dotnet user-secrets set --project DevTrack.Api
Write-Output $learningPassword
```

For a learning container, user secrets stay on the host. Pass `DatabaseProvider=InMemory` and supply `AdminAuth__Username`, `AdminAuth__Password`, and `Jwt__SigningKey` as container environment variables. The API settings file supplies the issuer, audience, and expiration defaults.

```powershell
docker build -t devtrack:learning .
$apiSecretId = 'DevTrack.Api-9cc9fc2d-7b6b-4997-9e11-60b75f6a327a'
$learningSecrets = Get-Content (Join-Path $env:APPDATA "Microsoft/UserSecrets/$apiSecretId/secrets.json") -Raw | ConvertFrom-Json
$learningEnvFile = New-TemporaryFile
try {
    @(
        'DatabaseProvider=InMemory'
        "AdminAuth__Username=$($learningSecrets.'AdminAuth:Username')"
        "AdminAuth__Password=$($learningSecrets.'AdminAuth:Password')"
        "Jwt__SigningKey=$($learningSecrets.'Jwt:SigningKey')"
    ) | Set-Content -LiteralPath $learningEnvFile.FullName
    docker run --rm -p 127.0.0.1:8080:8080 --env-file $learningEnvFile.FullName devtrack:learning
} finally {
    Remove-Item -LiteralPath $learningEnvFile.FullName
}
```

Open `http://localhost:8080` to sign in. Learning data resets when the container stops.

## Technology Stack
- **Framework**: .NET 10 (C# 14)
- **Database**: SQL Server with EF Core
- **API Client**: IHttpClientFactory
- **Frontend**: ASP.NET Core MVC with modern CSS styling
- **Solution Format**: Visual Studio `.slnx`


### Dynamic RBAC for MVC and API

Adapted from `DotNetTrainingBatch0.MvcCookieAuthDynamicRbac` and
`DotNetTrainingBatch0.JwtDynamicRbacWebApi`. Users have one database role;
roles receive permissions through `RolePermissions`. Passwords use salted PBKDF2-SHA256.
MVC signs users in with a cookie and carries an API JWT in its authentication ticket.
The API also supports direct login through `POST /api/auth/login` with
`{"username":"...","password":"..."}`. The API checks current database grants for API operations and answers the MVC
permission check for each protected action. Disabling a user, changing a role, or
removing a grant applies to existing sessions immediately. JWT permission claims are informational;
they do not override database grants.

Set `DatabaseProvider=SqlServer` and `ConnectionStrings__DefaultConnection` on the API. Run `rbac_schema.sql` once before starting the API. The default InMemory provider is for development only. Its data belongs to the API process and is reset on restart.

Set `AdminAuth__Username`, `AdminAuth__Password`, and optionally
`AdminAuth__DisplayName` on the API to bootstrap the first administrator.
The initializer creates that user only when the Users table is empty, grants the
Admin role all known permissions, and never resets existing passwords or grants.
After bootstrap, credentials are validated against Users, rather than configuration.
API JWT issuance requires `Jwt__SigningKey` (at least 32 characters),
`Jwt__Issuer`, `Jwt__Audience`, and `Jwt__ExpiresMinutes`.

Permissions: `dashboard.read`, `batches.read`, `batches.write`,
`developers.read`, `developers.write`, `training.read`, `training.write`.
Read permissions cover lists, details, schedules, and reports; write permissions
cover creation, batch developer assignments, and attendance marking.
Attendance screens also load batches, so their roles need `batches.read`.
Manage roles and grants in the database. Create additional users with a password hash
produced by `RbacService.HashPassword`, a valid RoleId, and IsActive=true.
No user or role management UI is included.
