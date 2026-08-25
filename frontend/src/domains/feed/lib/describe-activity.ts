/**
 * Когда человек последний раз заходил — словами, без точных дат: в дейтинге
 * точное время последнего визита это слежка, а не польза.
 * `null` — сервер не отдал `lastActive`, значит про активность сказать нечего.
 */
export function describeActivity(lastActive: string | null): 'today' | 'week' | 'long' | null {
  if (lastActive === null) return null

  const seen = new Date(lastActive).getTime()
  if (Number.isNaN(seen)) return null

  const days = (Date.now() - seen) / (24 * 60 * 60 * 1000)
  if (days < 1) return 'today'

  return days < 7 ? 'week' : 'long'
}
