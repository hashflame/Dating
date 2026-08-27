/**
 * Сверено с backend: `Blizka.App/Domain/Enums/ReportReason.cs` (T-17.1).
 * Энумы сериализуются camelCase-строками.
 */
export type ReportReason =
  'fakePhotos' | 'scam' | 'underage' | 'insults' | 'explicit' | 'spam' | 'unsafeMeeting'

/**
 * Порядок как на макете S-13: сначала то, на что жалуются чаще.
 *
 * `as const satisfies` — литеральные ключи i18n сохраняются (типизированный
 * `t()` иначе их не примет), а `satisfies` следит за значениями энума.
 */
export const REPORT_REASONS = [
  { value: 'fakePhotos', labelKey: 'feed.safety.reason.fakePhotos' },
  { value: 'scam', labelKey: 'feed.safety.reason.scam' },
  { value: 'underage', labelKey: 'feed.safety.reason.underage' },
  { value: 'insults', labelKey: 'feed.safety.reason.insults' },
  { value: 'explicit', labelKey: 'feed.safety.reason.explicit' },
  { value: 'spam', labelKey: 'feed.safety.reason.spam' },
  { value: 'unsafeMeeting', labelKey: 'feed.safety.reason.unsafeMeeting' },
] as const satisfies ReadonlyArray<{ value: ReportReason; labelKey: string }>

/** Строка списка «Заблокированные» (S-51). Сверено с `Blizka.Api/Blocks/BlockedUserResponse.cs`. */
export type BlockedUser = {
  userId: string
  name: string
  mainPhotoUrl: string | null
  blockedAt: string
}
