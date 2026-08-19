import js from '@eslint/js'
import boundaries from 'eslint-plugin-boundaries'
import importX from 'eslint-plugin-import-x'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import prettier from 'eslint-config-prettier/flat'
import globals from 'globals'
import tseslint from 'typescript-eslint'

/** Слайс импортируется только через свой index.ts (публичный API слайса). */
const publicApi = (type) => ({ to: { element: { type, fileInternalPath: 'index.ts' } } })

/** Любой файл слоя — для shared разрешены глубокие импорты сегментов. */
const anyFileOf = (type) => ({ to: { element: { type } } })

/**
 * Слои архитектуры (сверху вниз). Импортировать можно только вниз.
 *
 *   app      → всё
 *   pages    → widgets, domains, shared
 *   widgets  → domains, shared
 *   domains  → domains (через index.ts), shared
 *   shared   → shared
 */
export default tseslint.config(
  { ignores: ['dist', 'coverage', 'node_modules', 'src/shared/ui/kit/**'] },

  js.configs.recommended,
  tseslint.configs.recommendedTypeChecked,
  {
    languageOptions: {
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
      globals: globals.browser,
    },
  },

  // ── Общий стиль кода ──────────────────────────────────────────────────────
  {
    files: ['**/*.{ts,tsx}'],
    rules: {
      'no-console': ['warn', { allow: ['warn', 'error'] }],
      eqeqeq: ['error', 'always', { null: 'ignore' }],
      'object-shorthand': 'error',
      'prefer-const': 'error',
      'no-nested-ternary': 'error',
      'no-param-reassign': 'error',

      '@typescript-eslint/consistent-type-imports': [
        'error',
        { prefer: 'type-imports', fixStyle: 'inline-type-imports' },
      ],
      '@typescript-eslint/consistent-type-definitions': ['error', 'type'],
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/no-non-null-assertion': 'error',
      '@typescript-eslint/array-type': ['error', { default: 'array-simple' }],

      // Именованные экспорты вместо default — предсказуемые имена для агента и IDE.
      'no-restricted-exports': ['error', { restrictDefaultExports: { direct: true } }],
    },
  },

  // ── React ─────────────────────────────────────────────────────────────────
  {
    files: ['**/*.{ts,tsx}'],
    extends: [reactHooks.configs.flat['recommended-latest'], reactRefresh.configs.vite],
  },

  // ── Импорты ───────────────────────────────────────────────────────────────
  {
    files: ['**/*.{ts,tsx}'],
    plugins: { 'import-x': importX },
    settings: {
      'import/resolver': {
        typescript: { project: './tsconfig.app.json' },
      },
      'import-x/resolver': {
        typescript: { project: './tsconfig.app.json' },
      },
    },
    rules: {
      'import-x/no-cycle': ['error', { maxDepth: 5 }],
      'import-x/no-self-import': 'error',
      'import-x/no-duplicates': 'error',
      'import-x/order': [
        'error',
        {
          groups: ['builtin', 'external', 'internal', 'parent', 'sibling', 'index'],
          pathGroups: [{ pattern: '@/**', group: 'internal', position: 'before' }],
          'newlines-between': 'always',
          alphabetize: { order: 'asc', caseInsensitive: true },
        },
      ],
      // Только алиасы: относительные пути наружу своего слайса запрещены.
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['../../*'],
              message: 'Используй алиас @/… вместо глубоких относительных путей.',
            },
          ],
        },
      ],
    },
  },

  // ── Архитектура: слои и публичные API ─────────────────────────────────────
  {
    files: ['src/**/*.{ts,tsx}'],
    plugins: { boundaries },
    settings: {
      // Слой = элемент. Слайс = папка внутри слоя. Сегмент = папка внутри слайса.
      'boundaries/elements': [
        { type: 'app', pattern: 'src/app' },
        { type: 'pages', pattern: 'src/pages/*', capture: ['slice'] },
        { type: 'widgets', pattern: 'src/widgets/*', capture: ['slice'] },
        { type: 'domains', pattern: 'src/domains/*', capture: ['slice'] },
        { type: 'shared', pattern: 'src/shared/*', capture: ['segment'] },
      ],
    },
    rules: {
      // Каждый файл в src обязан принадлежать слою.
      'boundaries/no-unknown-files': 'error',
      // Импорт из неопознанного места запрещён.
      'boundaries/no-unknown-dependencies': 'error',
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          message:
            'Нарушение архитектуры: {{from.type}} → {{to.type}}. Проверь направление слоёв и импортируй слайс через его index.ts',
          policies: [
            {
              from: { element: { type: 'app' } },
              allow: [
                publicApi('pages'),
                publicApi('widgets'),
                publicApi('domains'),
                anyFileOf('shared'),
              ],
            },
            {
              from: { element: { type: 'pages' } },
              allow: [publicApi('widgets'), publicApi('domains'), anyFileOf('shared')],
            },
            {
              from: { element: { type: 'widgets' } },
              allow: [publicApi('widgets'), publicApi('domains'), anyFileOf('shared')],
            },
            {
              from: { element: { type: 'domains' } },
              allow: [publicApi('domains'), anyFileOf('shared')],
            },
            {
              from: { element: { type: 'shared' } },
              allow: [anyFileOf('shared')],
            },
          ],
        },
      ],
    },
  },

  // ── Конфиги ───────────────────────────────────────────────────────────────
  {
    files: ['*.{js,ts}', 'vite.config.ts'],
    languageOptions: { globals: globals.node },
    extends: [tseslint.configs.disableTypeChecked],
    rules: { 'no-restricted-exports': 'off' },
  },

  // ── Расширение типов библиотек ────────────────────────────────────────────
  {
    files: ['**/*.d.ts'],
    rules: {
      '@typescript-eslint/consistent-type-definitions': 'off',
      '@typescript-eslint/no-empty-object-type': 'off',
    },
  },

  prettier,
)
