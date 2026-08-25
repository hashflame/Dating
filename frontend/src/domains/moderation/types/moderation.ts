/** Сверено с backend: `spec.md` §21.1 «Типы жалоб (S-13)». */
export type ReportReason =
  'fake_photos' | 'scam' | 'underage' | 'insults' | 'explicit' | 'spam' | 'unsafe_meeting'

/**
 * Порядок как в спеке: сначала то, на что жалуются чаще.
 *
 * `as const satisfies` — литеральные ключи i18n сохраняются (типизированный
 * `t()` иначе их не примет), а `satisfies` следит за значениями энума.
 */
export const REPORT_REASONS = [
  { value: 'fake_photos', labelKey: 'feed.safety.reason.fakePhotos' },
  { value: 'scam', labelKey: 'feed.safety.reason.scam' },
  { value: 'underage', labelKey: 'feed.safety.reason.underage' },
  { value: 'insults', labelKey: 'feed.safety.reason.insults' },
  { value: 'explicit', labelKey: 'feed.safety.reason.explicit' },
  { value: 'spam', labelKey: 'feed.safety.reason.spam' },
  { value: 'unsafe_meeting', labelKey: 'feed.safety.reason.unsafeMeeting' },
] as const satisfies ReadonlyArray<{ value: ReportReason; labelKey: string }>
