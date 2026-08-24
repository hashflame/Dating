import { type FeedCard } from '../types/feed'

/**
 * Расстояние для подписи, округлённое до километра.
 *
 * `null` — показывать только город: либо расстояние скрыто, либо оно меньше
 * километра, и «0 км от вас» выглядело бы ошибкой вместо «тот же город».
 */
export function distanceInKm(card: Pick<FeedCard, 'distanceKm'>): number | null {
  if (card.distanceKm === null) return null

  const km = Math.round(card.distanceKm)

  return km < 1 ? null : km
}
