# Platform (shell foundation)

Shared app foundation for the modular frontend.

| Area | Path |
|------|------|
| API / tokens | `api/` |
| Auth context + types | `auth/` |
| RBAC helpers | `permissions/rbac.ts` |
| Launcher / nav aggregation | `permissions/apps.ts` |
| Module contract | `module/types.ts` |
| Search registry | `search/` |
| Shell chrome | `shell/` |
| Shared UI | `ui/` |
| Org directory API (F4) | `org/` |

**Rule (ADR 0001):** platform code must not import `@modules/*`.
