/** Сверено с backend: `Blizka.Api/Referrals/ReferralDtos.cs` (T-20.1, S-47). */
export type ReferralInvite = {
  /** Реферальный код — он же хвост ссылки, показываем его в интерфейсе. */
  code: string
  /** `https://t.me/{bot}?start=ref_{code}` — этим делятся с друзьями. */
  deepLink: string
  /** Текст для шаринга, локализован сервером по языку запроса. */
  shareText: string
}

/** Ответ `GET /api/referrals/stats`. */
export type ReferralStats = {
  /** Всего зарегистрировавшихся по ссылке. */
  invited: number
  /** Из них дошедших до конца онбординга — за них начислены зорки. */
  registered: number
  sparksEarned: number
}
