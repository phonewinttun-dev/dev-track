-- Run once against the existing DevTrack SQL Server database before starting either host.
CREATE TABLE Roles (Id INT IDENTITY PRIMARY KEY, RoleName NVARCHAR(100) NOT NULL UNIQUE);
CREATE TABLE Permissions (Id INT IDENTITY PRIMARY KEY, PermissionName NVARCHAR(100) NOT NULL UNIQUE);
CREATE TABLE Users (
 Id INT IDENTITY PRIMARY KEY, Username NVARCHAR(150) NOT NULL UNIQUE,
 DisplayName NVARCHAR(MAX) NOT NULL, PasswordHash NVARCHAR(MAX) NOT NULL,
 RoleId INT NOT NULL REFERENCES Roles(Id), IsActive BIT NOT NULL
);
CREATE TABLE RolePermissions (
 RoleId INT NOT NULL REFERENCES Roles(Id), PermissionId INT NOT NULL REFERENCES Permissions(Id),
 PRIMARY KEY (RoleId, PermissionId)
);
