// `import type`, а не инлайновый `import { type … }`: инлайновую форму сборщик
// считает обычным статическим импортом, кладёт SDK в главный чанк — и
// динамический `import()` ниже перестаёт что-либо делить.
import type { PostHog } from 'posthog-js'

import { env } from '@/shared/config'

/**
 * Единственное место импорта `posthog-js` — как `@tma.js` живёт только
 * в `shared/telegram`. Остальной код зовёт `track()`, а не SDK.
 *
 * Два правила, из которых сделан этот модуль:
 *
 * 1. Аналитика не роняет приложение и не заставляет себя ждать. SDK грузится
 *    динамическим `import()` уже после первого рендера, а до его появления
 *    вызовы не падают.
 * 2. Аналитика выключается пустым ключом. Тогда чанк SDK не скачивается вовсе
 *    и наружу не уходит ни одного запроса.
 */

/** `null` — SDK ещё не загружен или аналитика выключена. */
let client: PostHog | null = null

/** Идёт загрузка: второй `initAnalytics()` не должен начать её заново. */
let starting = false

/**
 * События, случившиеся до конца загрузки SDK. Без очереди систематически
 * терялись бы первые экраны (`splash`, `welcome`) — то есть начало воронки
 * онбординга, ради которой всё и заводится.
 *
 * Ограничение на размер — страховка от утечки, если инициализация не дойдёт
 * до конца: без ключа сюда вообще ничего не кладётся.
 */
const MAX_PENDING = 50
let pending: Array<{ name: string; props: Record<string, unknown> }> = []

/** Загружает и настраивает SDK. Повторные вызовы — no-op. */
export async function initAnalytics(): Promise<void> {
  if (!env.analytics.enabled || client !== null || starting) return
  starting = true

  try {
    const { posthog } = await import('posthog-js')

    posthog.init(env.analytics.key, {
      api_host: env.analytics.host,
      defaults: '2026-08-30',
      // Клики по всему подряд съедят месячный лимит и не дадут ничего
      // осмысленного: события заводятся руками, вместе с местом вызова.
      autocapture: false,
      // Роутер на memory history — URL не меняется, и автоматический pageview
      // был бы один на всю сессию. Экраны шлём сами (`screen_viewed`).
      capture_pageview: false,
      capture_pageleave: false,
      // На cookies внутри Telegram-вебвью полагаться нельзя.
      persistence: 'localStorage',
      // Профиль человека появляется только после identify — до согласия
      // личность не идентифицируется.
      person_profiles: 'identified_only',
      // На экранах чужие анкеты и переписка. Понадобится — включать точечно
      // и с маскированием, отдельной задачей.
      disable_session_recording: true,
    })

    client = posthog
    flush()
  } catch {
    // Аналитика не взлетела — приложению всё равно. Копить события дальше
    // незачем: SDK уже не появится.
    pending = []
  } finally {
    starting = false
  }
}

export function capture(name: string, props: Record<string, unknown>): void {
  if (client === null) {
    queue(name, props)
    return
  }

  client.capture(name, props)
}

export function identify(id: string, props: Record<string, unknown>): void {
  client?.identify(id, props)
}

/** Забывает человека: следующие события уйдут новому. */
export function reset(): void {
  client?.reset()
}

function queue(name: string, props: Record<string, unknown>): void {
  if (!env.analytics.enabled || pending.length >= MAX_PENDING) return

  pending.push({ name, props })
}

function flush(): void {
  const queued = pending
  pending = []
  queued.forEach((event) => client?.capture(event.name, event.props))
}
