# Project rules

Follow [AGENTS.md](AGENTS.md) for global code formatting, naming, logging, error handling, and completion checks.
Follow the project architecture guide and the existing workflow/task pattern: workflows orchestrate meaningful tasks, check expected-failure Results, and own stop/continue decisions. Keep task implementations in the operation's Tasks folder and use the existing region formatting.

## Access tokens

- Issue signed HS256 JWT access tokens with exactly one payload claim: `sid`, generated from 32 cryptographically secure random bytes encoded as Base64URL. Do not add identity, permission, issuer, audience, or lifetime claims; expiry and session validity are enforced in the server database on every request.
- API session persistence stores only SHA-256 token hashes. Never log raw tokens or hashes. The client must retain the original credential for transport; MVC keeps it inside its encrypted authentication cookie, never in a session database or plaintext browser storage.
- Resolve user identity, roles, and permissions from current server data. Never encode them in access tokens. The protected `/me` endpoint may return the current user's profile and permissions, without password hashes, token hashes, or signing keys.
- Login responses must contain only `accessToken`. Never add user IDs, usernames, display names, roles, or permission lists to the login payload or MVC authentication claims. MVC loads current details and permissions from `/me` once per authenticated MVC request and checks endpoint requirements on the server; login and permission-check endpoints must never return grant lists; only protected `/me` returns the current user's grants. An independent random MVC session nonce may be used for antiforgery binding; it must not contain or derive from user data.
- Verify the JWT signature, allowed algorithm, and exact single-claim payload before looking up the hash. Validate session expiration, revocation, and active user status on every authenticated request. Resolve current identity and permissions from server data.
- Revoke the API session on logout. Let cancellation propagate and log and rethrow unexpected authentication or permission failures.
- Keep database entities and mappings in Database, and session business workflows in Domain.
- Build affected projects and run relevant security and workflow tests before completion.

## Simplicity and responsibility

- Apply SRP, KISS, and YAGNI: keep each component focused and make the normal flow easy to read.
- Use Result for expected failures and public service contracts. Private tasks that can only succeed or throw should return their value or Task directly; do not manufacture success-only Results or unreachable failure branches.
- Keep workflow/task separation for meaningful operations. Do not add forwarding helpers, generic frameworks, interfaces, or extra layers without a concrete reuse or responsibility benefit.
- Log useful outcomes and unexpected failures; avoid narrating every assignment or repeating the same outcome at multiple layers.

## Easy-to-remember names

- Use short, familiar words for filenames and class names. Prefer clear action names such as `TokenCheckHandler`, `TokenSendHandler`, `SessionService`, and `UserAccessService`.
- Keep filenames and class names aligned. Avoid unfamiliar abbreviations and protocol terms when ordinary words describe the responsibility clearly.

## Database startup

- SQLite is the default database. Keep automatic creation and missing-account setup in Domain `DatabaseSetup`, with supporting tasks in `Tasks/`.
- Startup adds the requested admin and staff accounts only when missing. Never reset existing password hashes, account status, roles, or permission changes on restart.
- Keep generated SQLite database files out of Git. Use isolated databases in tests.

## JWT signing key

- Keep signing keys separate from session records. Startup creates a random local `devtrack.key` when no Base64 `AccessToken:SigningKey` is configured.
- Never commit or log signing keys. Keep the local key file across restarts; containers must persist it alongside the SQLite database or configure a stable external key.

## Web current user

- Keep current-user details in a scoped `CurrentUser` object for the current request only; never add them to cookies, JWT claims, or browser storage.
- Load `/me` through the Auth API client's workflow and tasks. Use one global permission filter so metadata attributes never cause duplicate profile requests.
- Use the same current permissions for server-side page checks and visible actions. The API continues enforcing permissions independently.
- On `/me` HTTP 401, clear the MVC cookie and challenge. Other API failures or invalid profile payloads return a server error; never treat them as empty permissions or ordinary access denials. Let cancellation propagate.
