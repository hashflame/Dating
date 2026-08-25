/**
 * Расстояние для подписи, округлённое до километра.
 *
 * `null` — показывать только город: расстояние скрыто, его нет, либо оно меньше
 * километра, и «0 км от вас» выглядело бы ошибкой вместо «тот же город».
 * Метры не показываем сознательно: это уже про адрес, а не про район.
 */
export function distanceInKm(distance: number | null | undefined): number | null {
  if (distance === null || distance === undefined) return null

  const km = Math.round(distance)

  return km < 1 ? null : km
}
