# Feature modules

Each business capability lives under its own folder and exports a
`ModuleDefinition` that `src/app/modules.tsx` registers.

```
modules/<feature>/
  index.tsx          # ModuleDefinition (routes, navItems, appModule, searchProviders)
  pages/
  components/
  services/
  types/
```

**Rule (ADR 0001):** a module may import `@platform/*` and its own files only.
Do not import `@modules/<other>/*` (shell composition belongs in `src/app/`).
