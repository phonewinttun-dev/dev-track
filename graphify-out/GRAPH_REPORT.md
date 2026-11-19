# Graph Report - dev-track  (2026-11-20)

## Corpus Check
- Corpus is ~29,703 words - fits in a single context window. You may not need a graph.

## Summary
- 1038 nodes · 2511 edges · 78 communities (49 shown, 29 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 362 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Feature Contracts and DTOs
- Regression and Integration Tests
- API Endpoints
- Build and Configuration
- Regression and Integration Tests
- MVC Pages and Clients
- API Endpoints
- Regression and Integration Tests
- Build and Configuration
- MVC Pages and Clients
- API Endpoints
- Feature Contracts and DTOs
- MVC Pages and Clients
- EF Core Persistence
- Feature Contracts and DTOs
- Project Documentation
- Project Documentation
- Account Access and RBAC
- EF Core Persistence
- MVC Pages and Clients
- Regression and Integration Tests
- EF Core Persistence
- EF Core Persistence
- MVC Pages and Clients
- Feature Contracts and DTOs
- MVC Pages and Clients
- Regression and Integration Tests
- EF Core Persistence
- MVC Pages and Clients
- Feature Contracts and DTOs
- EF Core Persistence
- EF Core Persistence
- Regression and Integration Tests
- Vendor Browser Libraries
- Build and Configuration
- EF Core Persistence
- EF Core Persistence
- Feature Contracts and DTOs
- MVC Pages and Clients
- Project Documentation
- EF Core Persistence
- Training Business Rules
- Feature Contracts and DTOs
- MVC Pages and Clients
- Project Documentation
- Training Business Rules
- Vendor Browser Libraries
- Build and Configuration
- Build and Configuration
- Shared Results and Security
- MVC Pages and Clients
- MVC Pages and Clients
- Project Documentation
- Feature Contracts and DTOs
- Build and Configuration
- MVC Pages and Clients
- MVC Pages and Clients
- MVC Pages and Clients
- MVC Pages and Clients
- MVC Pages and Clients
- MVC Pages and Clients
- MVC Pages and Clients
- MVC Pages and Clients
- MVC Pages and Clients
- MVC Pages and Clients
- MVC Pages and Clients
- jQuery license terms
- MIT license for jquery-validation
- MIT license for jquery-validation-unobtrusive

## God Nodes (most connected - your core abstractions)
1. `Result` - 72 edges
2. `DevTrackDbContext` - 46 edges
3. `Batch` - 41 edges
4. `DateOnly` - 40 edges
5. `PaginationRequest` - 38 edges
6. `DevTrack.Shared` - 37 edges
7. `Developer` - 35 edges
8. `PagedResult` - 31 edges
9. `AttendanceRecord` - 30 edges
10. `DevTrack.Database.Entities` - 29 edges

## Surprising Connections (you probably didn't know these)
- `TrainingController` --references--> `TrainingService`  [INFERRED]
  DevTrack.Api/Controllers/TrainingController.cs → DevTrack.Domain/Features/Training/TrainingService.cs
- `TrainingApiClient` --references--> `TrainingController`  [INFERRED]
  DevTrack.WebApp/Services/TrainingApiClient.cs → DevTrack.Api/Controllers/TrainingController.cs
- `PermissionFilter` --references--> `RbacService`  [EXTRACTED]
  DevTrack.Api/Auth/PermissionAttribute.cs → DevTrack.Domain.Access/Features/Auth/RbacService.cs
- `AuthController` --references--> `RbacService`  [EXTRACTED]
  DevTrack.Api/Controllers/AuthController.cs → DevTrack.Domain.Access/Features/Auth/RbacService.cs
- `BatchesController` --references--> `IBatchService`  [EXTRACTED]
  DevTrack.Api/Controllers/BatchesController.cs → DevTrack.Models/Features/TrainingManagement/Batches/IBatchService.cs

## Import Cycles
- None detected.

## Communities (78 total, 29 thin omitted)

### Community 0 - "Feature Contracts and DTOs"
Cohesion: 0.05
Nodes (43): DeveloperResponse, DeveloperCode, Email, FullName, Id, IsActive, ITrainingService, AttendanceSummaryResponse (+35 more)

### Community 1 - "Regression and Integration Tests"
Cohesion: 0.06
Nodes (43): DevTrack.Api, net10.0, Microsoft.EntityFrameworkCore.InMemory (10.0.11), Microsoft.NET.Sdk.Web, DevTrack.Database, net10.0, Microsoft.EntityFrameworkCore.InMemory (10.0.11), Microsoft.NET.Sdk (+35 more)

### Community 2 - "API Endpoints"
Cohesion: 0.06
Nodes (16): ConfigureJwtBearerOptions, JwtTokenService, PermissionAttribute, Permission, PermissionFilter, AuthController, AdminJwtTokenServiceTests, JwtOptions (+8 more)

### Community 3 - "Build and Configuration"
Cohesion: 0.12
Nodes (7): DevTrack.WebApp.Auth, DevTrack.Api.Auth, DevTrack.WebApp.Controllers, DevTrack.Shared.Security, DevTrack.Api.Controllers, AuthConstants, Permissions

### Community 4 - "Regression and Integration Tests"
Cohesion: 0.10
Nodes (14): DeveloperService, DeveloperServiceTests, ServiceExceptionHandlingTests, FailingSaveContext, RecordingLogger, Exception, Level, Message (+6 more)

### Community 5 - "MVC Pages and Clients"
Cohesion: 0.12
Nodes (8): DevTrack.Features.TrainingManagement.Developers, DevTrack.Features.TrainingManagement.Dashboard, DevTrack.Features.TrainingManagement.Training, DevTrack.WebApp.Services, DevTrack.Features.TrainingManagement.Batches, DevTrack.Shared, AttendanceSummaryRegressionTests, Csv

### Community 6 - "API Endpoints"
Cohesion: 0.15
Nodes (4): BatchesController, DashboardController, DevelopersController, TrainingController

### Community 7 - "Regression and Integration Tests"
Cohesion: 0.17
Nodes (7): DevTrack.WebApp, ApiAssemblyMarker, ApiAuthenticationTests, WebAppAuthenticationTests, LoginRequest, LoginResponse, WebAppAssemblyMarker

### Community 8 - "Build and Configuration"
Cohesion: 0.11
Nodes (6): DevTrack.Features.Access.Auth, DevTrack.Domain.Access, DevTrack.Api, DevTrack.Domain, AccessModule, DbInitializer

### Community 9 - "MVC Pages and Clients"
Cohesion: 0.17
Nodes (6): BulkAttendanceRequest, BatchId, Markings, TrainingDate, Result, Data

### Community 10 - "API Endpoints"
Cohesion: 0.07
Nodes (28): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+20 more)

### Community 11 - "Feature Contracts and DTOs"
Cohesion: 0.08
Nodes (26): DeveloperAttendanceRecordResponse, Remark, Status, TrainingDate, DeveloperAttendanceSummary, AbsentDays, AttendancePercent, LateDays (+18 more)

### Community 12 - "MVC Pages and Clients"
Cohesion: 0.08
Nodes (25): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+17 more)

### Community 13 - "EF Core Persistence"
Cohesion: 0.10
Nodes (19): BatchDeveloper, Batch, BatchId, Developer, DeveloperId, Id, Developer, AssignmentSubmissions (+11 more)

### Community 14 - "Feature Contracts and DTOs"
Cohesion: 0.11
Nodes (18): AttendanceTrendModel, Date, PresentCount, BatchSummaryModel, AverageAttendance, BatchName, DeveloperCount, Id (+10 more)

### Community 15 - "Project Documentation"
Cohesion: 0.11
Nodes (24): Historical assignment and leave workflow gaps, Historical attendance rules gap, Historical batch creation contract gap, Historical dashboard attendance metric review, Historical developer self-service gap, Historical feature gap review (May 2026), Historical training calendar editing gap, Approve Leave Requests (+16 more)

### Community 16 - "Project Documentation"
Cohesion: 0.16
Nodes (22): Administrator bootstrap, Attendance System, Batch Management, Calendar Auto-Generation, Class-day attendance eligibility, Cookie authentication ticket, Developer Tracking, DevTrack.Api composition root (+14 more)

### Community 18 - "EF Core Persistence"
Cohesion: 0.14
Nodes (9): DevTrackDbContext, AssignmentSubmissions, AttendanceRecords, BatchDevelopers, Batches, Developers, LeaveRecords, TrainingCalendars (+1 more)

### Community 19 - "MVC Pages and Clients"
Cohesion: 0.18
Nodes (3): IBatchService, AttendanceController, BatchesController

### Community 20 - "Regression and Integration Tests"
Cohesion: 0.32
Nodes (4): TrainingService, TrainingServiceTests, DevTrack.Features.TrainingManagement.Training.BulkAttendanceRequest, DateOnly

### Community 21 - "EF Core Persistence"
Cohesion: 0.12
Nodes (16): Batch, AssignmentSubmissions, AttendanceRecords, BatchDevelopers, BatchName, CreatedAt, DaysPerWeek, EndDate (+8 more)

### Community 22 - "EF Core Persistence"
Cohesion: 0.15
Nodes (14): AppPermission, Id, PermissionName, AppRole, Id, RoleName, AppRolePermission, Permission (+6 more)

### Community 23 - "MVC Pages and Clients"
Cohesion: 0.19
Nodes (4): IDeveloperService, DevelopersController, DeveloperApiService, IDeveloperApiClient

### Community 24 - "Feature Contracts and DTOs"
Cohesion: 0.13
Nodes (14): AttendanceRequest, BatchId, DeveloperId, Remark, Status, TrainingDate, TrainingCalendarRequest, AssignmentDueDate (+6 more)

### Community 26 - "Regression and Integration Tests"
Cohesion: 0.19
Nodes (4): DevTrack.Domain.Tests, AuthTestSettings, ApiConfiguration, TestDbContextFactory

### Community 27 - "EF Core Persistence"
Cohesion: 0.18
Nodes (11): TrainingCalendar, AssignmentDueDate, AssignmentTitle, Batch, BatchId, DayType, Id, IsAttendanceRequired (+3 more)

### Community 28 - "MVC Pages and Clients"
Cohesion: 0.19
Nodes (3): IDashboardService, HomeController, DashboardApiService

### Community 29 - "Feature Contracts and DTOs"
Cohesion: 0.15
Nodes (12): DevTrack.Features.TrainingManagement.Batches.BatchRequest, BatchRequest, BatchName, DaysPerWeek, EndDate, MentorName, MinAttendancePercent, MonthlyLeaveAllowed (+4 more)

### Community 32 - "EF Core Persistence"
Cohesion: 0.18
Nodes (11): AssignmentSubmission, Batch, BatchId, Developer, DeveloperId, Feedback, Id, IsSubmitted (+3 more)

### Community 34 - "Vendor Browser Libraries"
Cohesion: 0.22
Nodes (4): escapeAttributeValue(), onError(), onReset(), validationInfo()

### Community 35 - "Build and Configuration"
Cohesion: 0.18
Nodes (10): key, sync, value, services, dockerContext, dockerfilePath, env, envVars (+2 more)

### Community 36 - "EF Core Persistence"
Cohesion: 0.20
Nodes (10): AttendanceRecord, Batch, BatchId, CreatedAt, Developer, DeveloperId, Id, Remark (+2 more)

### Community 37 - "EF Core Persistence"
Cohesion: 0.20
Nodes (10): LeaveRecord, ApprovedAt, ApprovedBy, Batch, BatchId, Developer, DeveloperId, Id (+2 more)

### Community 38 - "Feature Contracts and DTOs"
Cohesion: 0.29
Nodes (5): BatchAssignmentModel, DeveloperCode, DeveloperId, FullName, IsAssigned

### Community 40 - "Project Documentation"
Cohesion: 0.53
Nodes (9): AssignmentSubmissions table, AttendanceRecords table, BatchDevelopers table, Batches table, Core Tables schema notes, Developers table, Illustrative attendance SQL calculation, LeaveRecords table (+1 more)

### Community 41 - "EF Core Persistence"
Cohesion: 0.22
Nodes (9): AppUser, DisplayName, Id, IsActive, PasswordHash, Role, RoleId, Username (+1 more)

### Community 42 - "Training Business Rules"
Cohesion: 0.33
Nodes (3): DashboardService, DashboardServiceTests, TrainingManagementModule

### Community 43 - "Feature Contracts and DTOs"
Cohesion: 0.22
Nodes (8): BatchResponse, BatchName, DateRange, EndDate, Id, MentorName, StartDate, TrainingMonths

### Community 44 - "MVC Pages and Clients"
Cohesion: 0.31
Nodes (3): BatchApiClient, BatchApiService, IBatchApiClient

### Community 45 - "Project Documentation"
Cohesion: 0.25
Nodes (6): Async service contracts, Build and test verification, Domain database initialization ownership, Feature file organization, Repository engineering conventions, Structured logging

### Community 46 - "Training Business Rules"
Cohesion: 0.25
Nodes (7): AttendanceMetrics, AbsentDays, AttendancePercent, LateDays, LeaveDays, PresentDays, Status

### Community 47 - "Vendor Browser Libraries"
Cohesion: 0.36
Nodes (5): f(), n(), p(), a(), u()

### Community 48 - "Build and Configuration"
Cohesion: 0.68
Nodes (7): AssignmentSubmissions, AttendanceRecords, BatchDevelopers, Batches, Developers, LeaveRecords, TrainingCalendar

### Community 50 - "Shared Results and Security"
Cohesion: 0.29
Nodes (4): Result, IsFailure, IsSuccess, Message

### Community 51 - "MVC Pages and Clients"
Cohesion: 0.33
Nodes (4): DevTrack.WebApp.Models, ErrorViewModel, RequestId, ShowRequestId

### Community 52 - "MVC Pages and Clients"
Cohesion: 0.33
Nodes (4): LoginViewModel, Password, ReturnUrl, Username

### Community 53 - "Project Documentation"
Cohesion: 0.40
Nodes (4): Controller to domain to database flow, General architecture guide (historical), Historical Blazor example, Service responsibility

### Community 54 - "Feature Contracts and DTOs"
Cohesion: 0.40
Nodes (5): AttendanceMarkingModel, DeveloperId, FullName, Remark, Status

### Community 55 - "Build and Configuration"
Cohesion: 0.70
Nodes (4): Permissions, RolePermissions, Roles, Users

## Knowledge Gaps
- **341 isolated node(s):** `Structured logging`, `Async service contracts`, `Feature file organization`, `Build and test verification`, `Domain database initialization ownership` (+336 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 425 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **29 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DevTrackDbContext` connect `EF Core Persistence` to `EF Core Persistence`, `EF Core Persistence`, `EF Core Persistence`, `EF Core Persistence`, `EF Core Persistence`, `EF Core Persistence`, `EF Core Persistence`, `EF Core Persistence`?**
  _High betweenness centrality (0.008) - this node is a cross-community bridge._
- **What connects `Structured logging`, `Async service contracts`, `Feature file organization` to the rest of the system?**
  _341 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Feature Contracts and DTOs` be split into smaller, more focused modules?**
  _Cohesion score 0.05185185185185185 - nodes in this community are weakly interconnected._
- **Why does `DeveloperService` connect `Regression and Integration Tests` to `Feature Contracts and DTOs`, `MVC Pages and Clients`, `EF Core Persistence`, `MVC Pages and Clients`, `Training Business Rules`?**
  _High betweenness centrality (0.005) - this node is a cross-community bridge._
- **Should `Regression and Integration Tests` be split into smaller, more focused modules?**
  _Cohesion score 0.06025369978858351 - nodes in this community are weakly interconnected._
- **Why does `Batch` connect `EF Core Persistence` to `EF Core Persistence`, `EF Core Persistence`, `EF Core Persistence`, `Feature Contracts and DTOs`, `EF Core Persistence`, `Regression and Integration Tests`, `EF Core Persistence`?**
  _High betweenness centrality (0.004) - this node is a cross-community bridge._
- **Should `API Endpoints` be split into smaller, more focused modules?**
  _Cohesion score 0.059233449477351915 - nodes in this community are weakly interconnected._
## Flow and extraction notes

- [System flow diagrams](SYSTEM_FLOW.md)
- Semantic extraction ran in-session. The worker did not provide metered token counts.
- Graph diagnostics: {'dangling_endpoint_edges': 114, 'self_loop_edges': 3, 'directed_same_endpoint_collapsed_edges': 93}
