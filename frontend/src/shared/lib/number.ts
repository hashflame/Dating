/** Зажимает значение в границы. Нужен при драге карточек и прогрессе. */
export function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max)
}

/** Расстояние с единицей измерения: «800 м», «2,4 км», «15 км». */
export function formatDistanceKm(km: number, locale: string): string {
  if (km < 1) {
    return new Intl.NumberFormat(locale, {
      style: 'unit',
      unit: 'meter',
      maximumFractionDigits: 0,
    }).format(Math.round(km * 1000))
  }

  return new Intl.NumberFormat(locale, {
    style: 'unit',
    unit: 'kilometer',
    maximumFractionDigits: km < 10 ? 1 : 0,
  }).format(km)
}
