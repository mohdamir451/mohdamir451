# PDF Comparison UI - ASP.NET Core 8 (Authentication & User Management)

This project provides an ASP.NET Core 8 starter implementation for:

- Login & JWT-based authentication
- Password hashing and secure storage
- Logout/session handling
- User management module (create/edit/disable users)
- Role assignment (Admin, Reviewer, User)
- RBAC policies and audit fields
- UI screens for login, invalid login state, user list, add/edit user

## Tech

- ASP.NET Core 8 MVC + API endpoints
- Entity Framework Core SQLite
- Hybrid auth: Cookie (MVC) + JWT bearer (API)

## Run (when .NET 8 SDK is available)

```bash
dotnet restore
dotnet run
```

Default route:

- `/Auth/Login`
- `/Comparison/Index` (PDF Validation Workbench)

Demo seeded users:

- `admin` / `Admin@123`
- `reviewer` / `Reviewer@123`

API login endpoint:

- `POST /api/auth/login`


## Improvement checklist

See `docs/IMPROVEMENTS_REQUIRED.md` for a prioritized hardening and production-readiness checklist.


Health endpoints:

- `/health`
- `/health/ready`


Comparison API endpoints:

- `POST /api/comparison/upload-pdf`
- `POST /api/comparison/compare`
- `POST /api/comparison/submit`
- `POST /api/comparison/export-excel`
