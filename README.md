# DevTrack Management System

DevTrack is a comprehensive management system designed to track training batches, developers, and attendance. It is built using a modern, decoupled architecture with .NET 10, following the **Result Pattern** and leveraging **IHttpClientFactory** for API-driven communication.

## Project Structure

DevTrack has one backend, a single Domain project containing the Training Management and Access modules, and a separate ASP.NET Core MVC frontend.

- **DevTrack.Api** is the backend composition root. It registers domain features through `AddFeatures()`, exposes thin controllers, configures the database, and calls the Domain startup setup.
- **DevTrack.Domain** owns the `Dashboard`, `Batches`, `Developers`, `Training`, and `Auth` features. The Attendance menu maps to `Training`, which also owns the schedule and reports. `Auth` owns credentials, roles, permissions, and bootstrap. `FeatureManager.cs` registers all domain feature services with scoped lifetimes.
- **DevTrack.Models** holds typed requests, responses, and feature service contracts. Its `Features/<Module>/<Feature>` folders mirror the domain features and share their namespaces.
- **DevTrack.Database** owns the EF Core context and persistence mappings used by the domain modules.
- **DevTrack.Shared** holds the result types, pagination, and shared constants.
- **DevTrack.WebApp** references Models and reaches the API over HTTP for feature data, login, and live permissions. It owns the MVC views and browser cookie.

### Supporting files

- **[Documentation](docs/README.md)** groups architecture, requirements, database reference, reviews, and workflow guides.
- **[Database scripts](DevTrack.Database/Scripts/README.md)** separates schema setup from sample seed data.
- **graphify-out/** contains generated architecture reports and visualizations.
- Solution, SDK, deployment, and repository instruction files stay at the root for their tools to discover.

### Adding a feature

Place business logic in the owning module's `Features/<Feature>` folder and shared DTOs in its matching Models folder. Register the service in `FeatureManager.AddFeatures()` and expose it through a thin API controller. Keep secondary actions such as schedule and export in the menu's feature. If one domain module needs another, put the interface and DTO in Models and register its implementation through the feature manager.

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
- No database server installation is needed; SQLite is included.

### Configuration
1. **Database**: SQLite is the default. Running the API creates `DevTrack.Api/devtrack.db`, its tables, sample training data, and the accounts below. Set `ConnectionStrings:DefaultConnection` to use another SQLite file; relative paths are resolved from the API folder.
2. **API URL**: Update the `ApiSettings:BaseUrl` in `DevTrack.WebApp/appsettings.json` to match the API's hosting URL.
3. **Login**: Startup adds missing `admin` / `admin@123` and `staff` / `staff@123` accounts. Admin has full access; staff has read-only access. Passwords are stored as salted hashes. Restarting does not reset existing passwords, roles, or grants.
4. **Access tokens**: Configure a positive `AccessToken:ExpiresMinutes` in `DevTrack.Api` (default 480). Sessions use signed JWTs containing only a random `sid` claim. Startup creates `devtrack.key` automatically when `AccessToken:SigningKey` is not configured. Expiration stays in the session database.

### Running the Application
1. Start the **DevTrack.Api** project.
2. Start the **DevTrack.WebApp** project.
3. Access the system via the WebApp URL.

### Local setup

Run the API and WebApp, then sign in with either account:

| Username | Password | Access |
| --- | --- | --- |
| `admin` | `admin@123` | Full access |
| `staff` | `staff@123` | Read-only |

No SQL scripts or user secrets are needed. `DatabaseSetup.cs` in Domain creates
the database and adds missing accounts. SQLite keeps data between restarts.

For a container:

```powershell
docker build -t devtrack:local .
docker run --rm -p 127.0.0.1:8080:8080 --mount type=volume,source=devtrack-data,target=/data -e "ConnectionStrings__DefaultConnection=Data Source=/data/devtrack.db" -e "AccessToken__KeyFile=/data/devtrack.key" devtrack:local
```

Open `http://localhost:8080`. The volume keeps the database when the container stops.

## Technology Stack
- **Framework**: .NET 10 (C# 14)
- **Database**: SQLite with EF Core
- **API Client**: IHttpClientFactory
- **Frontend**: ASP.NET Core MVC with modern CSS styling
- **Solution Format**: Visual Studio `.slnx`


### Dynamic RBAC for MVC and API

Adapted from `DotNetTrainingBatch0.MvcCookieAuthDynamicRbac` and
`DotNetTrainingBatch0.JwtDynamicRbacWebApi`. Users have one database role;
roles receive permissions through `RolePermissions`. Passwords use salted PBKDF2-SHA256.
MVC signs users in with a cookie and carries a signed JWT access token in its authentication ticket.
The API also supports direct login through `POST /api/auth/login` with
`{"username":"...","password":"..."}`. The API checks current database grants for API operations and answers the MVC
permission lookup through `/me`. The WebApp loads current user details once per authenticated page request and checks endpoint requirements on the server. Disabling a user, changing a role, or
removing a grant applies to existing sessions immediately. Tokens contain no identity or permission claims. The API stores only SHA-256 token hashes and resolves current identities and grants on the server. Login returns only `accessToken`. MVC builds an authenticated ticket without identity, role, or permission claims and uses its server-side API client to load `/me`. Current details are stored in server memory only for that request and drive the display name, role, navigation, and edit actions.

The default `DatabaseProvider=SQLite` uses `Data Source=devtrack.db`.
`DatabaseSetup.InitializeAsync` creates the schema and adds missing startup data
inside a transaction. It preserves existing user settings and permission changes.
`DatabaseProvider=InMemory` is retained for isolated tests; those databases reset
when the process stops. Old SQL Server scripts remain as historical references.

Set `AccessToken__ExpiresMinutes` to control session lifetime (default 480). `POST /api/auth/logout` revokes the supplied bearer session; MVC logout calls this endpoint before clearing its cookie. Expired, revoked, and disabled-user sessions return HTTP 401. Previous opaque tokens and JWTs with identity claims are no longer accepted; sign in again after deployment. Expired or revoked session records can be deleted by a maintenance job.

Permissions: `dashboard.read`, `batches.read`, `batches.write`,
`developers.read`, `developers.write`, `training.read`, `training.write`.
Read permissions cover lists, details, schedules, and reports; write permissions
cover creation, batch developer assignments, and attendance marking.
Attendance screens also load batches, so their roles need `batches.read`.
Manage roles and grants in the database. Create additional users with a password hash
produced by `UserAccessService.HashPassword`, a valid RoleId, and IsActive=true.
No user or role management UI is included.

### JWT payload

Login still returns only `accessToken`. Its decoded JWT payload contains exactly:

```json
{ "sid": "<32 random bytes encoded as Base64URL>" }
```

The signature uses HS256. The API verifies the signature and looks up the hash of
the complete JWT to enforce expiry, revocation, active user status, and current
permissions. No user ID, name, role, permissions, or expiry is encoded in the JWT.
This is a server-backed session token; it must always be checked against this API's
session database rather than accepted using its signature alone.

`AccessToken:SigningKey` accepts at least 32 random bytes encoded as Base64. When
unset, startup creates or reuses `devtrack.key` in the API folder. Keep that file
private and preserve it across restarts. `AccessToken:KeyFile` can select another
path. The key file is ignored by Git and is separate from database session rows.

### Swagger login

In Development, open `/swagger` on the API. Call `POST /api/auth/login`, copy the
`accessToken`, then select **Authorize** and paste the token without `Bearer `.
Swagger adds the authorization header to protected requests. Login remains public.

### Session and health checks

- `GET /me` requires a valid JWT and returns the current `userId`, `username`,
  `displayName`, `role`, and `permissions` from the database. Profile and grant
  changes appear on the next request, without changing the JWT. Password hashes,
  token hashes, and signing keys are excluded. Invalid, expired, revoked, or
  disabled-user sessions receive HTTP 401.
- `GET /health` is public and returns `{ "status": "healthy" }` when the API is
  running. This is a simple availability check, not a database readiness check.

Both endpoints appear in Swagger; only `/me` requires **Authorize**.
