# Database scripts

The application now uses SQLite and creates its database automatically through Domain `DatabaseSetup`. No scripts are needed to run it.

The SQL Server scripts below are historical references; they are not used by SQLite startup.

## Schema

Apply these scripts in order:

1. [Schema/init_db.sql](Schema/init_db.sql): training tables and attendance uniqueness index.
2. [Schema/rbac_schema.sql](Schema/rbac_schema.sql): users, roles, and permissions.
3. [Schema/access_sessions_schema.sql](Schema/access_sessions_schema.sql): opaque access sessions.

See the [project README](../../README.md) for current SQLite setup.

## Sample data

[Seed/data.sql](Seed/data.sql) inserts sample training data after schema setup. Review it before running against an existing database; it uses explicit IDs and is intended for sample data setup.
