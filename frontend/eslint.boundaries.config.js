/**
 * CI-focused ESLint config: ADR 0001 import boundaries only.
 * Full style lint remains `npm run lint`.
 */
import boundaries from 'eslint-plugin-boundaries';
import { defineConfig, globalIgnores } from 'eslint/config';
import tseslint from 'typescript-eslint';

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
            group: ['../../modules/*', '../../../modules/*', '**/../modules/*'],
            message:
              'ADR 0001: modules must not import sibling modules via relative paths.',
          },
        ],
      },
    ],
  },
}));

export default defineConfig(
  globalIgnores(['dist', 'src/modules/_template/**']),
  {
    files: ['src/**/*.{ts,tsx}'],
    languageOptions: {
      parser: tseslint.parser,
    },
    plugins: {
      boundaries,
    },
    settings: {
      'boundaries/include': ['src/**/*.{ts,tsx}'],
      // Bootstrap / theme live outside feature boundaries.
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
  {
    files: ['src/app/**/*.{ts,tsx}', 'src/main.tsx'],
    rules: {
      'no-restricted-imports': 'off',
    },
  },
);
