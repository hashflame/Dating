import { RotateCcw } from 'lucide-react'

import { resetDevUser } from '@/shared/telegram'

/**
 * Начинает онбординг заново.
 *
 * Эндпоинта, который откатывает состояние, у API нет, поэтому сбрасываем иначе:
 * выдаём новый Telegram-id, и следующий вход создаёт чистый аккаунт. Прошлый
 * тестовый аккаунт остаётся в базе — это цена отсутствия сброса на бэкенде.
 */
export function DevResetOnboarding() {
  const handleReset = (): void => {
    resetDevUser()
    // Полная перезагрузка, а не навигация: initData собирается один раз при
    // старте, и новый id должен попасть в него до первого запроса.
    window.location.reload()
  }

  return (
    <button
      type="button"
      onClick={handleReset}
      aria-label="Сбросить онбординг (только для разработки)"
      className="flex size-9 items-center justify-center rounded-full border border-border bg-card/90 text-muted-foreground shadow-sm backdrop-blur transition-colors hover:bg-accent"
    >
      <RotateCcw className="size-4" aria-hidden />
    </button>
  )
}
