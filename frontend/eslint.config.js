import js from '@eslint/js';
import { defineConfig, globalIgnores } from 'eslint/config';
import boundaries from 'eslint-plugin-boundaries';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';

/** Feature modules under src/modules (ADR 0001). */
const FEATURE_MODULES = [
  'auth',
  'leave',
  'expense',
  'fleet',
  'coreHr',
  'users',
  'reporting',
  'settings',
  'notifications',
  'support',
  'help',
];

function siblingModuleImportPatterns(ownModule) {
  return FEATURE_MODULES.filter((name) => name !== ownModule).flatMap((name) => [
    `@modules/${name}`,
    `@modules/${name}/*`,
  ]);
}

const perModuleBoundaryConfigs = FEATURE_MODULES.map((mod) => ({
  files: [`src/modules/${mod}/**/*.{ts,tsx}`],
  rules: {
    'no-restricted-imports': [
      'error',
      {
        patterns: [
          {
            group: siblingModuleImportPatterns(mod),
            message: `ADR 0001: module "${mod}" must not import sibling modules. Use @platform contracts or app composition.`,
          },
          {
            group: [
              '../../modules/*',
              '../../../modules/*',
              '**/../modules/*',
            ],
            message:
              'ADR 0001: modules must not import sibling modules via relative paths. Prefer @platform or app composition.',
          },
        ],
      },
    ],
  },
}));

export default defineConfig(
  globalIgnores(['dist', 'src/modules/_template/**']),
  {
    extends: [js.configs.recommended, tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
      boundaries,
    },
    settings: {
      'boundaries/include': ['src/**/*.{ts,tsx}'],
      'boundaries/ignore': ['src/main.tsx', 'src/theme.ts', 'src/theme.tsx'],
      'boundaries/elements': [
        { type: 'platform', pattern: 'src/platform/*' },
        { type: 'app', pattern: 'src/app/*' },
        {
          type: 'module',
          pattern: 'src/modules/*',
          capture: ['module'],
        },
      ],
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true },
      ],
      // Layer graph (relative + resolved local imports). Alias siblings are also
      // blocked by per-module no-restricted-imports below.
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          policies: [
            {
              from: { element: { type: 'platform' } },
              allow: { to: { element: { type: 'platform' } } },
            },
            {
              from: { element: { type: 'module' } },
              allow: { to: { element: { type: 'platform' } } },
            },
            {
              from: { element: { type: 'module' } },
              allow: {
                to: {
                  element: {
                    type: 'module',
                    captured: { module: '{{ from.element.captured.module }}' },
                  },
                },
              },
            },
            {
              from: { element: { type: 'app' } },
              allow: {
                to: { element: { type: ['app', 'platform', 'module'] } },
              },
            },
          ],
        },
      ],
    },
  },
  {
    files: ['src/platform/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@modules/*', '**/modules/*'],
              message:
                'ADR 0001: platform/shared code must not import from feature modules.',
            },
          ],
        },
      ],
    },
  },
  ...perModuleBoundaryConfigs,
  // Shell may compose modules.
  {
    files: ['src/app/**/*.{ts,tsx}', 'src/main.tsx'],
    rules: {
      'no-restricted-imports': 'off',
    },
  },
);
