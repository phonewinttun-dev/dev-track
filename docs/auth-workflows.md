# Authentication workflows

The existing public types, filenames and framework entry points remain unchanged.
JWT configuration lives under `Auth/Configure/`, token creation under
`Auth/CreateToken/`, and permission evaluation under `Auth/Authorize/`. Supporting
tasks live in each operation's `Tasks/` folder. `PermissionAttribute.cs` retains
the metadata attribute at its existing shared owner.

```mermaid
flowchart LR
    Host[API host] --> Configure[Configure bearer validation]
    Login[Login endpoint] --> Token[Create a signed token]
    Request[Protected request] --> Authorize[Check every required permission]
```

```mermaid
flowchart TD
    Configure[Configure] --> Scheme{Bearer scheme?}
    Scheme -->|No| Ignore[Leave options unchanged]
    Scheme -->|Yes| Parameters[Build existing validation parameters]
    Parameters --> Apply[Assign parameters]
    Token[Create token] --> Credentials[Prepare HS256 credentials]
    Credentials --> Build[Build JWT with existing claims and lifetime]
    Build --> Serialize[Serialize and return token Result]
    Authorize[Authorize request] --> Authenticated{Authenticated?}
    Authenticated -->|No| Challenge[Challenge]
    Authenticated -->|Yes| Metadata[Read required permission metadata]
    Metadata --> Identity{Numeric user ID?}
    Identity -->|No| Forbid[Forbid]
    Identity -->|Yes| Permissions[Check permissions in metadata order]
    Permissions --> Granted{Permission granted?}
    Granted -->|No| Forbid
    Granted -->|Yes, more permissions| Permissions
    Granted -->|All checked or none required| Continue[Continue request]
```

Each workflow receives a separate Result for each meaningful task, logs safe
outcomes, and owns the stop/continue decision. Authentication and permission
exceptions are logged and rethrown; cancellation propagates. No additional input
validation, permissions, response bodies, token claims, algorithms, lifetimes or
clock skew settings are introduced. Logs never contain token text, claims,
credentials or signing keys. Expected authorization failures keep the existing
challenge/forbid responses.

Token service methods now return `Result<string>` so failed task Results propagate
to the login controller, which maps failure to its server-error response. Framework
authorization filters likewise map failed Results to failure responses instead of
throwing them. Bearer validation parameters remain a simple projection with no
manufactured Result failure; unexpected configuration errors still propagate.
