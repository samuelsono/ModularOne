# Shared foundation

Shared libraries consumed by every `CarTrack.Modules.*` project and by `CarTrack.Host`.

| Project | Contents |
|---------|----------|
| `CarTrack.Core` | `IAuditable`, messaging contracts |
| `CarTrack.Infrastructure` | Auditable interceptor, module DbContext helpers, Aspire service defaults, CarTrack API client/models, in-process `IEventBus` |
| `CarTrack.Identity` | `ApplicationUser`, `JwtOptions`, `AppRoles`/`PermissionCatalog`, auth handlers, org/user scope contracts |
| `CarTrack.Api` | `IModule`, `ModuleRegistry`, `RequirePermission`, shared approval DTOs |

Many types keep historical namespaces (`CarTrack.Server.Data`, `.Users`, `.Auth`, `.CarTrack`) so EF Identity and call sites stay stable while assemblies move.

Composition root: **`CarTrack.Host`** (ex-`CarTrack.Server`).
