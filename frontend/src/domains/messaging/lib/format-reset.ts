/**
 * Когда обнулится недельный лимит: «до 5 сентября».
 *
 * Дата, а не «через N дней»: окно недельное, и точный день понятнее обратного
 * отсчёта, который на глазах устаревает.
 */
export function formatResetDate(iso: string, locale: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''

  return new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'long' }).format(date)
}
