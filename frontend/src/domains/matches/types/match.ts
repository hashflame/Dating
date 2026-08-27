/** Сверено с backend: `Blizka.Api/Matches/MatchDtos.cs`. */
export type UnlockedContact = {
  /** `null`, если у человека нет username — тогда открываем по deepLink. */
  telegramUsername: string | null
  deepLink: string | null
  sparksSpent: number
  sparksBalance: number
}

export type MatchUser = {
  userId: string
  name: string
  /** `null` — человек включил «Скрывать возраст» (T-16.1). */
  age: number | null
  mainPhotoUrl: string | null
}

/**
 * Три секции списка (S-30). Мэтч уходит в архив через 7 дней без переписки,
 * вернуть его можно бесплатно и всегда.
 */
export type Matches = {
  new: Array<{
    matchId: string
    user: MatchUser
    matchedAt: string
    /** Сколько зорок стоит открыть контакт. */
    contactCost: number
    /** У человека включено «Запретить писать мне» — он пишет первым сам. */
    writesFirst: boolean
    badge: string | null
  }>
  waitingForMessage: Array<{
    matchId: string
    user: MatchUser
    contactOpenedAt: string
    badge: string
  }>
  archived: Array<{
    matchId: string
    user: MatchUser
    archivedAt: string
    reason: string
  }>
}

/**
 * Открыт ли контакт. `locked` — нужно платить, `unlocked` — username доступен,
 * `writes_first_only` — человек включил «Запретить писать мне» (T-16.1, S-51):
 * платить не за что, первым он напишет сам.
 */
type ContactStatus = 'locked' | 'unlocked' | 'writes_first_only'

/**
 * Хаб мэтча (S-31) — центральный экран, из которого открываются ветки.
 * `telegramUsername` приходит только после оплаты.
 */
export type MatchHub = {
  matchId: string
  user: {
    userId: string
    name: string
    /** `null` — человек включил «Скрывать возраст» (T-16.1). */
    age: number | null
    city: string
    lastActive: string | null
    telegramUsername: string | null
    mainPhotoUrl: string | null
  }
  compatibility: { score: number; details: string }
  contactStatus: ContactStatus
  contactCost: number
  features: {
    questionOfDay: { available: boolean }
    minigame: { available: boolean }
    dateIdea: { available: boolean }
    staleConversation: { available: boolean }
  }
}

type QuestionAnswer = {
  text: string
  answeredAt: string
}

/**
 * Вопрос дня (S-37). `available: false` — вопроса на сегодня нет; ответы
 * открываются только когда ответили оба.
 */
export type QuestionOfDay = {
  available: boolean
  questionId: string | null
  questionText: string | null
  myAnswer: QuestionAnswer | null
  partnerAnswer: QuestionAnswer | null
}

export type QuestionArchiveItem = {
  questionId: string
  questionText: string
  publishedAt: string | null
  myAnswer: QuestionAnswer | null
  partnerAnswer: QuestionAnswer | null
}

/**
 * Идея свидания (S-39). Сверено с backend: `Blizka.Api/Matches/DateIdeaDtos.cs`
 * (T-12.1) — подбор из каталога по общим предпочтениям, не LLM-генерация.
 */
export type DateIdea = {
  title: string
  description: string
  estimatedCost: number
  /** Код валюты, три буквы: сервер всегда подписывает ответ своей. */
  currency: string
  /** Готовая строка вроде «2 часа» — сервер отдаёт её текстом. */
  estimatedDuration: string
  /** Текст приглашения, его и копируют в Telegram. */
  inviteText: string
}
