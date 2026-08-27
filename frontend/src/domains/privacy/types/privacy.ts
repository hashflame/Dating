/**
 * Настройки приватности (S-51). Сверено с backend:
 * `Blizka.Api/Privacy/PrivacySettingsDtos.cs` (T-16.1).
 *
 * Строки в БД может не быть — сервер тогда отдаёт умолчания, а не 404.
 */
export type PrivacySettings = {
  /** «Запретить писать мне»: у мэтчей появляется «пишет первой сама». */
  blockIncomingMessages: boolean
  hideDistance: boolean
  hideAge: boolean
  showLastActive: boolean
  /** Только для подписки «Безлимит»: включение без неё — 422. */
  invisibleMode: boolean
}

/** Не переданное поле сервер оставляет как есть — отсюда `Partial`. */
export type PrivacySettingsPatch = Partial<PrivacySettings>
