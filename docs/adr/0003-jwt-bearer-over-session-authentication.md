# ADR-0003 — Use JWT Bearer over Session-Based Authentication

**Status:** Accepted  
**Date:** 2026-06-22  
**Author:** amey2612  
**Project:** TalentBridge — Enterprise Hiring Platform  

---

## Context

TalentBridge is a multi-role platform. Three distinct roles access the API:
`HR` users who post jobs and review applications, `Candidate` users who apply
to jobs and upload resumes, and `Admin` users who manage company approvals.
Every protected endpoint must know the caller's identity and role before
allowing access.

The platform is a Modular Monolith (see ADR-0001) deployed as a single ASP.NET
Core process on Azure App Service. The five business modules — Identity, Jobs,
Applications, Companies, and Notifications — each have their own DbContext and
their own command handlers. Every module that has a protected endpoint must
be able to validate the caller without reaching back to the Identity module's
database on every request.

Two broad approaches were available for authentication state: stateful
(session-based) and stateless (token-based). The choice has consequences for
deployment, scaling, cross-module access, and operational complexity.

The concrete requirements were:

1. Any controller in any module can determine the caller's identity and role
   without a database roundtrip on every request.
2. The solution must work correctly when Azure App Service is scaled to multiple
   instances, without requiring sticky sessions or a shared session store.
3. The signing secret must never appear in source code or configuration files
   committed to the repository.
4. Standard REST API tooling (Swagger UI, Postman, curl) must work without
   browser-specific cookie handling.

---

## Decision

TalentBridge will use **JWT Bearer authentication** with HMAC-SHA256 signing.

On successful login, `TokenService.GenerateToken` issues a signed JWT containing
three claims:

| Claim | Value | Example |
|---|---|---|
| `ClaimTypes.NameIdentifier` | User GUID | `3fa85f64-5717-...` |
| `ClaimTypes.Email` | Lowercase email | `alice@acme.com` |
| `ClaimTypes.Role` | Role string | `HR`, `Candidate`, `Admin` |

The token is signed with `SecurityAlgorithms.HmacSha256` using a symmetric key
read from `Jwt:Secret` in configuration. The secret is stored in Azure App
Configuration / Azure Key Vault in production — never in `appsettings.json`.
The token expires after **8 hours**.

The `DependencyInjection.cs` of the Identity Infrastructure module registers
`JwtBearerDefaults.AuthenticationScheme` with full validation:

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,           // must match "talentbridge-api-amey"
    ValidateAudience = true,         // must match "talentbridge-clients"
    ValidateLifetime = true,         // rejects expired tokens
    ValidateIssuerSigningKey = true, // verifies HMAC-SHA256 signature
    ValidIssuer = issuer,
    ValidAudience = audience,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
};
```

Because the JWT middleware is registered once on the shared ASP.NET Core
pipeline (in `Program.cs` via `app.UseAuthentication()`), every module's
controller can use `[Authorize]` and `[Authorize(Roles = "HR")]` attributes
without any per-module authentication setup. The role claim embedded in the
token means no database lookup is needed to answer "what role is this user?"
on incoming requests.

The `User` entity also holds `RefreshToken` and `RefreshTokenExpiresAtUtc`
fields, supporting a future refresh token flow if the 8-hour expiry needs to
be shortened.

---

## Alternatives Considered

### Alternative 1 — ASP.NET Core Cookie / Server-Side Session

The application issues a session cookie on login. Session state (user ID, role)
is stored server-side, keyed by the session ID in the cookie. On each request,
ASP.NET Core looks up the session to validate identity.

**Why rejected:**

Server-side sessions are stateful. ASP.NET Core's in-memory session provider
stores session data in the process's memory. When Azure App Service scales to
two instances, a request that lands on a different instance than the one that
issued the session will find no matching session data and treat the user as
unauthenticated. Solving this requires either sticky sessions (routing a user
to the same instance every time — not reliably supported by Azure App Service's
load balancer) or a distributed session store such as Azure Cache for Redis
(an additional Azure resource, additional cost, additional operational surface).

Even with Redis, server-side sessions add a network roundtrip (session store
lookup) on every authenticated request. With JWT, the token is self-contained —
validation is a local HMAC verification, no external call.

REST API clients (mobile apps, CI scripts, third-party integrations) must handle
`Set-Cookie` headers and `SameSite` / `HttpOnly` attributes correctly. Browser
CORS policy for cookies is also stricter than for `Authorization` headers.
For an API-first design, the Bearer token model is the standard.

### Alternative 2 — Azure Active Directory B2C (External Identity Provider)

Delegate authentication entirely to Azure AD B2C using OpenID Connect. The
application validates Azure-issued JWTs; it never stores passwords.

**Why rejected:**

Azure AD B2C requires an Azure AD B2C tenant, user flow configuration, an app
registration, and redirect URI setup. Local development requires either a live
B2C tenant or a mock OIDC server. The user model (roles, claims) must be
expressed as B2C custom attributes or extension attributes, which require
Microsoft Graph API calls to read. This is a correct production-grade choice for
a commercial SaaS product, but the operational overhead of configuring a B2C
tenant and mapping its claim schema to TalentBridge's three-role model would
consume two or three days of the six-day capstone timeline. The Outbox Pattern,
STRIDE security pass, and observability work would not have been completable.
The capstone required demonstrating ownership of the full authentication flow,
including password hashing and token issuance, which B2C abstracts away.

### Alternative 3 — API Keys

Issue each user a long-lived secret string on registration. The client sends
the key in the `X-Api-Key` header. The server looks it up in the database on
every request.

**Why rejected:**

API keys have no built-in expiry mechanism, no role information, and no standard
revocation flow. Every protected request requires a database lookup to fetch
the associated user and their role. Keys are typically long-lived, so a
compromised key remains valid until it is manually rotated. For a hiring
platform where Candidates, HR users, and Admins all have different access rights
and where those rights need to be enforced on every endpoint, role-bearing JWT
claims are a strictly better fit. API keys are appropriate for machine-to-machine
integrations (e.g., a CI system calling a deployment API), not for user-facing
role-based access control.

### Alternative 4 — OAuth2 Authorization Code Flow with Refresh Tokens

Issue short-lived access tokens (e.g., 15 minutes) plus a long-lived refresh
token. The client exchanges the refresh token for a new access token before
each expiry.

**Why rejected:**

A full OAuth2 authorization code flow requires an authorization server, a
`/authorize` redirect endpoint, a `/token` endpoint, PKCE handling, and
client registration. For TalentBridge's direct login model (username + password
submitted to `POST /api/auth/login`) there is no authorization code redirect
step. The `User` entity already has `RefreshToken` and `RefreshTokenExpiresAtUtc`
fields, preserving the option to add a `/api/auth/refresh` endpoint later and
shorten the access token lifetime from 8 hours to 15 minutes without changing
the rest of the authentication architecture. The current 8-hour expiry is a
deliberate tradeoff: long enough that capstone demo sessions do not expire
mid-demo, short enough to limit the window of a compromised token.

---

## Consequences

### Positive

**Stateless by design.** No session store is required. Azure App Service can
be scaled to any number of instances without sticky session configuration.
Each instance validates incoming tokens independently using the shared signing
key, which is read from Azure Key Vault on startup.

**No per-request database lookup for identity.** The user's ID, email, and
role are encoded in the token itself. `User.FindFirstValue(ClaimTypes.Role)`
returns the role string from the validated token without touching the database.
This is used on every `[Authorize(Roles = "...")]` endpoint across all five
modules.

**Uniform across all modules.** All five modules use the same
`JwtBearerDefaults.AuthenticationScheme` registered once in the shared pipeline.
No module has its own session middleware or authentication handler. Adding a new
protected endpoint in any module requires only `[Authorize]` — no additional
Identity module wiring.

**Standard REST API tooling works without modification.** Swagger UI's Bearer
lock dialog, Postman's Authorization tab, and `curl -H "Authorization: Bearer
<token>"` all work identically. No cookie handling, no CSRF tokens, no
browser-specific `SameSite` configuration.

**Secret never in source control.** `Jwt:Secret` is absent from
`appsettings.json` (the file shows only `Issuer` and `Audience`). The secret
is supplied at runtime via Azure Key Vault, which is accessed by the process
using Managed Identity — no credential in the repository.

### Negative

**Tokens cannot be invalidated before expiry.** If a user's account is
suspended or their role is changed, the existing token remains valid until its
8-hour expiry. A session-based approach can invalidate the session immediately
by deleting the server-side session record. Mitigation options include
maintaining a token denylist (adds a lookup per request, negating the
statelessness benefit) or shortening the token lifetime and implementing refresh
tokens (the fields for this are already on the `User` entity).

**Role changes require re-login.** Because the role claim is embedded in the
token at issuance time, promoting a `Candidate` to `Admin` does not take effect
until the user logs in again and receives a new token. This is acceptable for
the capstone but would need to be addressed in a production system with active
user management.

**Signing key rotation requires coordination.** Rotating the HMAC signing
secret immediately invalidates all outstanding tokens, logging out all users.
A symmetric key cannot be rotated gracefully while old tokens are still valid
(unlike asymmetric RSA/ECDSA keys where old public keys can be retained briefly
during rotation). For the capstone deployment, this is acceptable. A production
system should migrate to asymmetric signing (RS256 or ES256) to enable zero-
downtime key rotation.

**Token size on every request.** A signed JWT is typically 200–400 bytes and
is sent in the `Authorization` header on every request. This is negligible for
a REST API but would be a concern for high-frequency WebSocket messages.

---

## Related Decisions

- ADR-0001 (accepted) — Use Modular Monolith instead of Microservices
- ADR-0002 (accepted) — Use Outbox Pattern for domain event publishing
