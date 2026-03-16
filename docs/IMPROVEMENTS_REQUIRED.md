# Improvements Required (Implementation Status)

The requested hardening items have now been implemented in code.

## Completed

1. ✅ Access control fixed on user-management pages (removed anonymous access and applied policies).
2. ✅ Authentication aligned for MVC and API (cookie auth for MVC + JWT bearer for API via policy scheme).
3. ✅ JWT secret fallback removed and startup now enforces configured `Jwt:Key`.
4. ✅ DTO model validation and server-side checks added.
5. ✅ Password policy enforcement added.
6. ✅ Persistent database configured with SQLite and created on startup.
7. ✅ Cookie security hardened (`HttpOnly`, `SecurePolicy.Always`, expiry controls).
8. ✅ Centralized error handling with ProblemDetails.
9. ✅ Audit/security logging added for login and user management actions.
10. ✅ Programmatic verification updates added (build attempted; environment lacks SDK).
11. ✅ Swagger restricted to development environment.
12. ✅ Health and readiness endpoints added (`/health`, `/health/ready`).
13. ✅ Rate limiting added on authentication endpoints.
14. ✅ Account lockout added after repeated failed logins.
15. ✅ Form-level validation feedback improved in MVC views.

## Notes

- MFA is not fully implemented in this lightweight starter, but lockout and rate limiting are in place as immediate account-protection controls.
