/** Ответ `POST /api/referrals/invite`. Эндпоинта ещё нет — см. docs/api-gaps.md. */
export type ReferralInvite = {
  /** Ссылка-приглашение с реферальным кодом, которой делятся с друзьями. */
  link: string
}
