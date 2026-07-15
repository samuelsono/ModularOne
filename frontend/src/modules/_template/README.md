# Frontend module template

1. Copy `_template/` → `../<moduleName>/` (e.g. `invoicing`).
2. Rename `templateModule` / routes / ids / permissions.
3. Register once in `src/app/modules.tsx`.
4. Add the module id to `FEATURE_MODULES` in `eslint.config.js` and `eslint.boundaries.config.js`.

**Do not** import `@modules/<other>/*`. Shared UI/API lives in `@platform/*`. Cross-feature screens are composed under `src/app/`.
