import type { DEFAULT_NAMESPACE, resources } from './config'

declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: typeof DEFAULT_NAMESPACE
    resources: (typeof resources)['ru']
  }
}
