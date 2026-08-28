/**
 * Каталог быстрых вопросов анкеты и их назначение пользователю.
 *
 * MVP-заглушка: реальный каталог и назначение должны переехать на бэкенд
 * (тикет ClickUp, см. `docs/api-gaps.md`, S-40). До тех пор вопросы
 * определяются на клиенте детерминированно по `userId` — один и тот же
 * пользователь всегда получает один и тот же набор из трёх вопросов, и любой
 * зритель анкеты умеет их пересчитать так же, не запрашивая ничего у сервера.
 */
export const QUICK_QUESTIONS_COUNT = 3

/** `as const`, чтобы `labelKey` сузился до литералов — тех же ключей i18n, что и в `t()`. */
export const QUICK_QUESTIONS = [
  { id: 'idealWeekend', labelKey: 'quickQuestions.idealWeekend' },
  { id: 'lastLaugh', labelKey: 'quickQuestions.lastLaugh' },
  { id: 'twoTruthsOneLie', labelKey: 'quickQuestions.twoTruthsOneLie' },
  { id: 'greenFlag', labelKey: 'quickQuestions.greenFlag' },
  { id: 'comfortFood', labelKey: 'quickQuestions.comfortFood' },
  { id: 'randomSkill', labelKey: 'quickQuestions.randomSkill' },
  { id: 'firstDateIdea', labelKey: 'quickQuestions.firstDateIdea' },
  { id: 'favoritePlace', labelKey: 'quickQuestions.favoritePlace' },
  { id: 'petPeeve', labelKey: 'quickQuestions.petPeeve' },
  { id: 'currentlyLearning', labelKey: 'quickQuestions.currentlyLearning' },
  { id: 'guiltyPleasure', labelKey: 'quickQuestions.guiltyPleasure' },
  { id: 'dreamTrip', labelKey: 'quickQuestions.dreamTrip' },
] as const

export type QuickQuestion = (typeof QUICK_QUESTIONS)[number]

/** Простой строковый хеш (djb2-подобный) — детерминированный и без зависимостей. */
function hashString(value: string): number {
  let hash = 5381

  for (let i = 0; i < value.length; i++) {
    hash = (hash * 33) ^ value.charCodeAt(i)
  }

  return hash >>> 0
}

/** Детерминированный псевдослучайный генератор от числового зерна (mulberry32). */
function seededRandom(seed: number): () => number {
  let state = seed

  return () => {
    state = (state + 0x6d2b79f5) | 0
    let t = Math.imul(state ^ (state >>> 15), 1 | state)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t

    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

/**
 * Три вопроса, закреплённые за пользователем навсегда: одно и то же `userId`
 * всегда даёт один и тот же набор в одном порядке.
 */
/** Есть ли хоть один непустой ответ — сами ответы хранятся по фиксированным позициям, пустых с конца не обрезают. */
export function hasAnsweredPrompt(prompts: readonly string[]): boolean {
  return prompts.some((prompt) => prompt.trim() !== '')
}

export function pickQuickQuestions(userId: string): readonly QuickQuestion[] {
  const random = seededRandom(hashString(userId))
  const pool: QuickQuestion[] = [...QUICK_QUESTIONS]
  const picked: QuickQuestion[] = []

  for (let i = 0; i < QUICK_QUESTIONS_COUNT && pool.length > 0; i++) {
    const [question] = pool.splice(Math.floor(random() * pool.length), 1)
    if (question) picked.push(question)
  }

  return picked
}
