import { type AnalyticsEvent } from './events'
import { capture, identify, reset } from './posthog'

/**
 * Единственная функция отправки события для всего кода.
 *
 * ```ts
 * track({ name: 'swipe_undone' })
 * track({ name: 'likes_revealed', cost: result.sparksSpent })
 * ```
 *
 * Ничего не возвращает и никогда не бросает: место вызова не должно знать,
 * включена ли аналитика и загрузился ли SDK.
 */
export function track(event: AnalyticsEvent): void {
  const { name, ...props } = event

  capture(name, props)
}

/**
 * Свойства человека: демография и платформа. Отправляются один раз при входе
 * и при изменении данных — дальше любой график в PostHog раскладывается по ним.
 *
 * `id` — внутренний `userId` бэкенда, а не Telegram-id: он и так псевдонимный,
 * наружу ничего лишнего не уезжает.
 */
export function identifyViewer(id: string, properties: Record<string, unknown>): void {
  identify(id, properties)
}

/**
 * Забыть текущего человека. Нужен при смене пользователя в панели разработки:
 * иначе события двух аккаунтов слипнутся в одного человека.
 */
export function resetAnalytics(): void {
  reset()
}
