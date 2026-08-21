import { type ParseKeys } from 'i18next'
import { useTranslation } from 'react-i18next'

/**
 * Переводит сообщение об ошибке поля. В zod-схемах хранятся ключи i18n,
 * но react-hook-form типизирует сообщение как обычную строку — отсюда каст.
 */
export function useFieldError(): (message: string | undefined) => string | undefined {
  const { t } = useTranslation()

  return (message) => (message ? t(message as ParseKeys) : undefined)
}
