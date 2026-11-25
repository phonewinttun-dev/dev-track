# Web authentication workflows

`Auth/Login/`, `Auth/Me/`, `Auth/Logout/`, and `Auth/ForwardToken/` contain the
operation workflows and their supporting `Tasks/`. Shared response validation is
under `Auth/Tasks/`. Private HTTP tasks return responses directly; workflow methods
return Results for expected HTTP or payload failures. Clients and responses are
disposed by the workflow that owns them, and cancellation propagates.

Login stores only the JWT and an independent antiforgery session nonce in the
protected cookie. The JWT itself contains only the random `sid` claim.

The global `PermissionFilter` skips anonymous endpoints. For each authenticated
MVC request, it calls `GET /me` once, validates the profile, and puts it in the
scoped `CurrentUser` object. This state lasts for one request; it is never added
to authentication claims or persisted in browser storage. `PermissionAttribute`
is metadata only, so multiple requirements do not trigger duplicate calls.

The filter checks all endpoint requirements against the current server-returned
permissions using ordinal case-insensitive matching. The layout displays the
current display name and role, and views hide unavailable links and edit actions.
The API still checks current grants independently for every protected operation.
The WebApp no longer calls the permission-decision endpoint.

A `/me` HTTP 401 signs out the MVC cookie and challenges. Other HTTP errors,
incomplete profiles, or invalid JSON produce a server error without sign-out or
a fake denial. Unexpected transport errors are logged and rethrown. Logs exclude
profile values, permission lists, JWTs, and response bodies.

Profile and grant changes appear on the next page request without issuing another
token. Logout still revokes the API session before clearing the cookie.
