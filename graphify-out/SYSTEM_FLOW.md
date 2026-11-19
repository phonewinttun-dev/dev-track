# DevTrack system flow

DevTrack runs an ASP.NET Core MVC frontend and an API backend. Razor renders HTML, Tailwind CSS styles the pages, and jQuery supports client-side scripts and validation. The shared mobile menu uses native JavaScript. The API owns authentication, authorization, training business rules, and persistence. Both apps use contracts from Models and result types from Shared.

## Architecture

```mermaid
flowchart LR
    Browser["Browser<br/>HTML, Tailwind CSS, jQuery"] <-->|Pages, forms, and session cookie| MVC["DevTrack.WebApp<br/>Razor views and MVC controllers"]
    MVC <-->|HTTP and JSON<br/>Bearer JWT after login| API["DevTrack.Api<br/>Controllers and composition root"]
    API --> Access["Access module<br/>Login and live permissions"]
    API --> Training["Training Management module<br/>Dashboard, Batches, Developers, Attendance"]
    Access --> EF["DevTrack.Database<br/>EF Core DbContext"]
    Training --> EF
    EF --> Database["SQL Server or learning-mode in-memory store"]
    MVC -.-> Models["DevTrack.Models<br/>DTOs and service contracts"]
    API -.-> Models
    Training -.-> Models
    Models -.-> Shared["DevTrack.Shared<br/>Result types, pagination, constants"]
```

In the combined container, the browser reaches MVC on port 8080 by default. MVC calls the API on container loopback port 5000. Render uses SQL Server. In-memory mode seeds sample training data and resets it when the API stops.

## Sign in and open a protected page

```mermaid
sequenceDiagram
    actor B as Browser
    participant W as MVC
    participant A as API
    participant R as Access module
    participant D as EF Core database
    B->>W: POST /Account/Login with form token
    W->>A: POST /api/auth/login
    A->>R: Validate credentials and load grants
    R->>D: Read user, role, and permissions
    D-->>R: Current account and grants
    A-->>W: Identity, grants, and signed JWT
    W-->>B: Issue session cookie and redirect
    B->>W: Request a protected page with cookie
    W->>A: GET /api/auth/permissions with JWT
    A->>R: Read current grants for token subject
    R->>D: Query current role permissions
    A-->>W: Current permission list
    alt Permission granted
        W->>A: Request feature data with JWT
        Note over A,D: Feature endpoint also checks its required permission
        A-->>W: Typed result
        W-->>B: Render Razor view
    else Permission denied
        W-->>B: Redirect to Access Denied
    end
```

Invalid credentials return the login form with an error. If the API rejects the JWT on a permission check, MVC clears the cookie and challenges the browser to sign in again.

## Record attendance

```mermaid
flowchart TD
    Form["Staff opens a batch class date and submits marks"]
    Mvc["MVC validates the form token and checks training.write"]
    Api["POST /api/training/attendance/bulk<br/>API checks token and training.write"]
    Validate["TrainingService checks class date, roster, unique developer IDs, and statuses"]
    Save["Update or insert marks with EF Core"]
    Refresh["MVC redirects to attendance summary"]
    Error["MVC displays the service error"]
    Form --> Mvc --> Api --> Validate
    Validate -->|Valid| Save --> Refresh
    Validate -->|Invalid| Error
```

The summary and CSV use the same attendance calculation. Eligibility uses the batch minimum, with 80% as the fallback when none is configured.

## Source entry points

| Area | Source |
| --- | --- |
| Container startup and ports | [Dockerfile](../Dockerfile) |
| API registration and initialization | [API Program](../DevTrack.Api/Program.cs) |
| MVC routing and antiforgery | [MVC Program](../DevTrack.WebApp/Program.cs) |
| Login and live grants | [API AuthController](../DevTrack.Api/Controllers/AuthController.cs), [MVC AccountController](../DevTrack.WebApp/Controllers/AccountController.cs), [RBAC service](../DevTrack.Domain.Access/Features/Auth/RbacService.cs) |
| Attendance validation and summary | [TrainingService](../DevTrack.Domain/Features/Training/TrainingService.cs), [AttendanceMetrics](../DevTrack.Domain/Features/Training/AttendanceMetrics.cs) |
| Database mappings | [DevTrackDbContext](../DevTrack.Database/Entities/DevTrackDbContext.cs) |

The [interactive graph](graph.html) provides the file and symbol relationships behind this overview. The [extraction report](GRAPH_REPORT.md) records how the graph was built.
