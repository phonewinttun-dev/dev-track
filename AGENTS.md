# Repository Instructions

## Clean code rules

Readable code over clever code. Simple code over unnecessary abstraction. Business logic must be explicit, testable, and easy for another developer to understand.

These rules apply to all projects in this repository.

### Naming

- Use `PascalCase` for classes, methods, properties, public members, and constants.
- Use `camelCase` for local variables, method parameters, and private fields.
- Use meaningful names and avoid abbreviations unless they are well-known.
- Boolean names must express a condition, such as `isActive`, `hasPermission`, or `canProcess`.

### Classes

- Keep classes small, cohesive, and focused on one responsibility.
- Avoid large classes that handle unrelated responsibilities.
- Group related members together and keep the public API surface minimal.

### Methods

- Give each method one clear responsibility and a descriptive name.
- Keep methods short and readable.
- Prefer early returns and guard clauses over deeply nested logic.

```csharp
if (request is null)
{
    return BadRequest();
}

if (!request.IsValid)
{
    return BadRequest();
}

return ProcessRequest(request);
```

### Parameters

- Avoid methods with too many parameters.
- Use a named request model when parameters belong together, such as `CreateUser(CreateUserRequest request)`.

### Null handling

- Validate required inputs early.
- Use nullable reference types and avoid unnecessary null checks.
- Do not silently ignore unexpected null values.

### Conditionals

- Keep conditions simple.
- Extract complex conditions into clearly named methods or variables, such as `IsActiveAdmin(user)`.

### Error handling

- Do not use exceptions for normal business flow; reserve them for exceptional situations.
- Use meaningful exception types and handle expected failures explicitly.
- Do not expose internal exception details to clients.
- Log unexpected exceptions with useful context; do not swallow them.
- Handle unexpected failures at public Domain service boundaries with `try`/`catch`, structured method-prefixed logs, and a safe failure result when the contract returns `Result` or `PagedResult`.
- Let cancellation propagate. Authentication, permission lookup, and startup seeding failures must be logged and rethrown rather than represented as invalid credentials, empty permissions, or successful initialization.

### Comments

- Prefer self-explanatory code.
- Comments must explain why, rather than restate what the code does.
- Do not comment obvious code.

### Magic values

- Replace unexplained hard-coded values with named constants or enums where appropriate.
- Prefer `status == UserStatus.Active` over `status == 1`.

### Logging

- Log meaningful business and technical information.
- Never log passwords, tokens, secrets, or sensitive personal data.
- Every log message must identify the exact method that emits it using `MethodName => message`.
- Require non-nullable `ILogger<T>` through constructor injection. Do not use optional logger parameters or `NullLogger<T>` fallbacks in production services; tests must explicitly supply a logger.
- Use structured logging placeholders instead of string interpolation for contextual values.
- Log exceptions through the logger's exception parameter.

```csharp
logger.LogInformation("Login => User login started. UserId={UserId}", userId);
logger.LogError(exception, "Login => Login failed. UserId={UserId}", userId);
```

### Async

- Implement asynchronous methods with `async Task` or `async Task<T>` and await the asynchronous operation, including forwarding methods.
- Interface method declarations continue to return `Task` or `Task<T>`; `async` belongs to the implementation.
- Do not block asynchronous operations with `.Result` or `.Wait()`.
- Use `CancellationToken` for long-running or I/O operations where appropriate.
- Use the `Async` suffix for asynchronous methods.

### LINQ

- Prefer readable LINQ and avoid expressions that are difficult to understand.
- Use a simple loop when it is clearer.
- Avoid unnecessary multiple enumeration.

### Duplication

- Do not duplicate business logic; reuse existing helpers and extract genuinely reusable logic.
- Do not introduce abstractions solely to avoid a few repeated lines.

### SOLID

- Apply Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion where they improve maintainability.
- Do not introduce patterns unnecessarily.

### Formatting

- Use the project's `.editorconfig`, when present, as the formatting source of truth.
- Use consistent indentation and brace placement, following the surrounding code where configuration does not specify a style.
- Use one statement per line and keep lines reasonably short.
- Separate logical sections with blank lines, remove unnecessary blank lines, and remove trailing whitespace.

### File structure

- Keep related code close together and organize files consistently with the existing project structure.
- Group feature code into controller, service, request model, response model, and DTO folders where appropriate.
- Do not reorganize unrelated files merely to enforce a preferred layout.

### Dead code

- Remove unused methods, variables, and `using` statements within the changed scope.
- Remove commented-out old implementations and obsolete code; do not retain them just in case.

### Refactoring

- Preserve existing behavior and do not change business rules unless explicitly requested.
- Improve readability and maintainability while reducing duplication.
- Keep changes focused on the requested task and reuse existing patterns.
- Do not introduce unnecessary architecture, dependencies, or design patterns.

### Code review and completion

Before considering a code change complete:

- Build the affected projects successfully and run relevant tests.
- Introduce no compiler warnings.
- Check for unused code and unnecessary duplication.
- Confirm logs contain no security-sensitive information and error handling is appropriate.
- Verify naming is clear and methods have a single responsibility.
- Verify existing behavior is preserved unless a behavior change was requested.
- Review the diff for unrelated changes and formatting errors.

### Database initialization

- Keep database initialization and seed orchestration in the Domain project. The Database project owns entities and persistence mappings.
- Use asynchronous Entity Framework operations in initializers and await initialization at startup.
