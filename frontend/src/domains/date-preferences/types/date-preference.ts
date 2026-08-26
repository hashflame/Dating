/** Сверено с backend: `Blizka.App/Domain/Enums/DatePreferenceCode.cs`. */
export type DatePreferenceCode =
  'activeOutdoors' | 'calmHangout' | 'quizzesBoardGames' | 'somethingNew'

export type DatePreference = {
  id: string
  /** Стабильный код, не зависит от локали — именно его принимает PATCH. */
  code: DatePreferenceCode
  name: string
}
