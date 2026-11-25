-- Apply once to an existing SQL Server database before deploying opaque token authentication.
CREATE TABLE AccessSessions (
    TokenHash VARCHAR(64) NOT NULL PRIMARY KEY,
    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    ExpiresAtUtc DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL
);
CREATE INDEX IX_AccessSessions_UserId ON AccessSessions(UserId);
CREATE INDEX IX_AccessSessions_ExpiresAtUtc ON AccessSessions(ExpiresAtUtc);
