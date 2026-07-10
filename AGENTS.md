# AGENTS.md - Sardanapal Identity

> Onboarding guide for AI agents and human contributors. Read this before touching the code.

---

## 1. Repo Rules (read first)

1. **Never commit CSV files.** `Issues.csv` (and any other `.csv`) tracks review work and must stay out of git. When staging, exclude CSV files explicitly.
2. **Workflow:** Plan first, then code, then commit in a separate step.
3. **Commit messages:** `<type>: <lowercase description>` where `type` is one of `resolve | update | release`. Never amend or force-push unless explicitly asked.
4. **Do not commit secrets** (connection strings, signing keys, nuget tokens).
5. **Never assume a library is available.** This solution depends on the `SardanapalCore` packages (`Sardanapal.Contract`, `Sardanapal.Domain`, `Sardanapal.Ef`, `Sardanapal.Service`, `Sardanapal.Http.Service`, `Sardanapal.RedisCache`, `Sardanapal.Share`, `Sardanapal.ViewModel`) referenced via central package management (`Directory.Packages.props`, `CoreVersion` = `1.0.0`).

---

## 2. Project Snapshot

- **What it is:** A .NET 8 identity-management **framework** shipped as NuGet packages (not a runnable app). Consumed by other solutions by subclassing its generic bases.
- **Version:** Package version `0.9.9` lives in `Build.json`. Core dependency version `1.0.0` lives in `Directory.Build.props` as `CoreVersion`.
- **Distribution:** Published to GitHub Packages at `https://nuget.pkg.github.com/harpm/index.json`.
- **Build/Release:** `SDBuild.go` (Go 1.21) reads `Build.json` and runs clean → restore → build (`-p:Version=`) → `dotnet nuget push` for each project. CI: `.github/workflows/dotnet.yml` triggers on push to `master` (needs .NET 8 + Go 1.21 + `GITHUBTOKEN` secret).
- **Companion repo:** `SardanapalCore` (sibling directory `../SardanapalCore`) holds the foundational abstractions. SardanapalIdentity extends them.

---

## 3. Code Style (enforced by `.editorconfig`)

- **Indentation:** 4 spaces, CRLF line endings, file ends with a newline.
- **Braces:** Allman style (`csharp_new_line_before_open_brace = all`), always use braces.
- **Types:** Prefer explicit types over `var` (suggestions are off for `var`).
- **Fields:** Private fields MUST be `_camelCase` (severity `error`). Modifier order: `public, private, protected, internal, static, ... async`.
- **File header template:** `All copy rights are reserved for Sardanapal Corp. Licensed under the MIT license.` (optional but expected).
- **Namespaces:** Block-scoped (`csharp_style_namespace_declarations = block_scoped`); `namespace matches folder` is a suggestion.
- Do **not** add comments unless explicitly requested.

---

## 4. Solution Map (11 projects)

Dependency arrows point toward what a project references.

| Project | Role | References |
|---|---|---|
| `Sardanapal.Identity.Share` | Config (`SDConfigs`), claim type constants/keys, password utilities | `Localization` + Core `Share` |
| `Sardanapal.Identity.Localization` | `.resx` resources + designers (`Identity_Messages`, `Identity_Exception`) | (none) |
| `Sardanapal.Identity.ViewModel` | Records/DTOs (`LoginVM`, `RegisterVM`, Otp VMs) + FluentValidation | `Share` + Core `ViewModel`/`RedisCache` |
| `Sardanapal.Identity.Contract` | Interfaces (services, repositories, models) | `ViewModel` + Core `Contract`/`Ef` |
| `Sardanapal.Identity.Authorization.Data` | `IdentityProvider` (per-request principal holder) | `Contract` |
| `Sardanapal.Identity.Authorization` | Middleware + action filters (`Authorize`, `HasRole`, `HasAccessRight`, `Ananymous`) | `Authorization.Data` |
| `Sardanapal.Identity.Domain` | Entities (`UserBase`, `RoleBase`, `ClaimBase`, `UserRole`, `UserClaim`), `SdIdentityUnitOfWork`, seeds | `Authorization.Data` + `Contract` + Core `Domain`/`Ef` |
| `Sardanapal.Identity.Repository` | EF + Memory repository bases for User/Role/Claim/OTP | `Contract` + Core `Ef`/`Service` |
| `Sardanapal.Identity.Services` | `TokenService`, `UserManager`, `RoleManager`, `AccountService`, `OtpAccountService`, `LoginAttemptTracker` | `Domain` + `Share` + Core `Service` |
| `Sardanapal.Identity.OTP` | `OTPModel` entity | `Contract` + Core `Domain` |
| `Sardanapal.Identity.OTP.Service` | OTP CRUD service + `OtpHelper` + Redis cache service | `Contract` + `OTP` + `ViewModel` + Core `Http.Service`/`RedisCache`/`Service`/`Ef` |

Typical app references: `Authorization` + `Services` (+ `OTP.Service` if OTP) and provides concrete subclasses of the domain/repository/service bases.

---

## 5. Core Architecture Patterns

### 5.1 Heavy generics + base classes

Consumers subclass generic bases binding their own key/entity types. Common generic params:
`TUserKey`, `TRoleKey` (always `byte`), `TClaimKey` (always `byte`), `TUser`, `TRole`, `TUR` (user-role link), `TUC` (user-claim link), `TClaim`.

Example hierarchies:
- `UserBase<TUserKey> → SardanapalUser<TUserKey, TRoleKey, TClaimKey>`
- `RoleBase<TKey> → SardanapalRole<TKey, TUserKey>`
- `ClaimBase<TKey>` → `ControllerActionClaimBase<TKey>` → `SardanapalClaim<TKey, TUserKey>`
- `UserRoleBase<TUserKey, TRoleKey>` / `UserClaimBase<TUserKey, TClaimKey>`
- `OTPModel<TUserKey, TKey>` (`TKey` is the OTP id, typically `Guid`)

### 5.2 EF vs Memory dual implementations

Most services/repos have BOTH an `EF*` variant (Entity Framework / DbContext) and a Memory/`Enumerable` variant. **When adding a repository or service method, implement it in both**:

- Repo: `EFUserRepositoryBase` vs `UserRepositoryBase`; `IEntityRepository` `IEFCrudRepository` vs `ICrudRepository`; `IEFOtpService` vs `OtpService`.
- Service: `EFUserManager` vs `UserManager`; `EFRoleManagerBase`; `EFOtpService` vs `OtpService`.
- Interfaces split too: `IEFUserRepository` (returns `IQueryable`) vs `IUserRepository` (returns `IEnumerable`).

### 5.3 Response envelope

Everything returns `IResponse` / `IResponse<T>` from Core. Key API:
- Construct: `new Response<T>(ServiceName, OperationType.<Fetch|Add|Edit|Delete|Function>, _logger)`.
- Wrap work in `result.Fill/FillAsync(() => { ... })`.
- Set outcome: `result.Set(StatusCode.<Succeeded|Failed|NotExists|Duplicate|Canceled|Exception>, dataOrMessage)`.
- Convert: `otherRes.ConvertTo<T>(result)`.
- Check: `result.IsSuccess`, `result.StatusCode`, `result.Data`.

### 5.4 Authentication pipeline

1. Client sends the raw JWT in a custom header `AUTH` (no `Bearer ` prefix). See `ConstantKeys.AUTH_HEADER_KEY`.
2. `SdAuthorizationMiddleware` reads the header → `IIdentityProvider.SetAuthorize(token)` → `ITokenService.ValidateToken` populates `Claims`.
3. Optional `SdAuthorizationMiddlewareWithRefreshToken` rebuilds and reissues a fresh token in the response `AUTH` header when the token is within `TokenRefreshThresholdMinutes` of expiry (≤ 0 disables). It reconstructs claim payloads from the expiring token so access is preserved.
4. Action filters (run by `Order`): `[Anonymous]` (0) → `[Authorize]` (1) → `[HasRole]` (3) → `[HasAccessRight]` (4). `[Authorize]` blocks tokens carrying `must_change_pw=true` with HTTP 403.

### 5.5 JWT claims

Claim type strings live in `Sardanapal.Identity.Share.Static.SdClaimTypes`:
- `id` — name identifier (user id).
- `sd_roles` — role id(s) as strings.
- `AccessRight` — claim id (AccessRight claim category).
- `sd_controller_action` — `"{ControllerId-Guid}:{ActionType-byte}"`.
- `must_change_pw` — `"true"` forces password change.

`SdClaimType` enum discriminates claim categories: `AccessRight = 1`, `ControllerAction = 2`.
`ClaimActionTypes` enum (Get/Add/Update/Delete) is the action type byte for ControllerAction claims.
`TokenService.GenerateToken`/`ValidateTokenRoles` take a non-generic `IClaim[]`. `TokenService.MapTokenClaims`/`HasClaims` switch on `claim.ClaimType` and cast to `IControllerActionClaim<byte>` (ControllerAction payload) or `IClaim<byte>` (AccessRight id). The refresh middleware reconstructs claims into a local `RefreshClaim : IControllerActionClaim<byte>` since it builds from token strings.

---

## 6. Security Invariants (do not regress)

These were deliberately fixed (see `Issues.csv`); any change must preserve them:
- **Passwords:** salted PBKDF2-HMAC-SHA256 (100k iters) via `Utilities.HashPassword` / `VerifyPassword` with `CryptographicOperations.FixedTimeEquals`. `EncryptToMd5` is `[Obsolete]`.
- **Anti-enumeration:** `Login`/`ChangePassword` verify against a `DummyPasswordHash` when the user is missing so timings match; return a generic `WrongPassword` message.
- **OTP:** `ValidateCode` rejects `ExpireTime <= UtcNow` and deletes the OTP record after a successful match (no replay). `Add` enforces a cooldown per (user, role).
- **Admin seed:** `IdentitySeeds.AddAdminUser` reads `SDConfigs.SeedAdminUsername/Password`, generates a strong random password when unset (logged as warning), and sets `MustChangePassword = true`.
- **Rate limiting:** `ILoginAttemptTracker` (default in-memory, `MaxLoginAttempts=5`, `LockoutMinutes=15`) gates login + all OTP flows. Register via `services.AddSardanapalAccountLockout()`.
- **Token refresh:** uses real token `Data`, only near expiry, preserves `must_change_pw`.

---

## 7. Localization

User-facing strings live in `Sardanapal.Identity.Localization`:
- Add keys to BOTH `Identity.Messages.resx` and `Identity.Messages.fa-IR.resx`, then regenerate the designer (`Identity.Messages.Designer.cs` is auto-generated via `PublicResXFileCodeGenerator`).
- Reference as `Identity_Messages.<Key>` (parameterized strings use `string.Format`).
- `Identity_Messages` is `public` (generated by `PublicResXFileCodeGenerator`); keep the resx generator setting so the class stays public for consumers.

---

## 8. Known Footguns

### Open work items (from `Issues.csv`, state `pending`)
- `C-9..C-11` design cleanups (static class fixups, etc.).
- `D-1` memory AddUserRole race.
- `E-1` no test project exists yet.

---

## 9. Local Build & Validation

- **Build solution:** `dotnet build "Sardanapal Identity.sln"` (Windows PowerShell). NuGet source for Core packages must be configured (GitHub Packages auth).
- **No test project** (`E-1`); verify by building and, where possible, by exercising the consuming app.
- After edits, run the solution build to confirm generics/contracts still resolve. There is no separate lint/typecheck command beyond the compiler.
