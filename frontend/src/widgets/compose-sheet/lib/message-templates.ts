import { type UserProfile } from '@/domains/profiles'

/**
 * Заготовка первого сообщения: на что опирается и чем заполнить текст.
 * Текст собирает вызывающий через `t` — здесь только выбор деталей анкеты.
 */
export type MessageTemplate =
  { id: string; kind: 'interest'; interest: string } | { id: string; kind: 'prompt'; quote: string }

/**
 * Заготовки первого сообщения из реальной анкеты собеседника (S-33).
 *
 * Это не AI: `POST /api/ai/generate-message` не реализован (см. api-gaps.md),
 * поэтому варианты собираются по правилам из спеки §9.2 — опора на конкретную
 * деталь анкеты, открытый вопрос в конце, ничего про внешность. Ничего не
 * выдумываем: нет ни интересов, ни ответов — заготовок не будет, и пользователь
 * напишет сам.
 */
export function buildMessageTemplates(profile: UserProfile): MessageTemplate[] {
  const templates: MessageTemplate[] = []

  for (const interest of profile.interests.slice(0, 2)) {
    templates.push({
      id: `interest-${interest.id}`,
      kind: 'interest',
      interest: interest.name.toLocaleLowerCase(),
    })
  }

  const prompt = profile.prompts.find((answer) => answer.trim() !== '')
  if (prompt !== undefined) {
    templates.push({ id: 'prompt', kind: 'prompt', quote: shorten(prompt) })
  }

  return templates
}

/** Цитата из анкеты в сообщении должна остаться цитатой, а не абзацем. */
function shorten(text: string): string {
  const trimmed = text.trim()
  const limit = 60

  return trimmed.length <= limit ? trimmed : `${trimmed.slice(0, limit).trimEnd()}…`
}
