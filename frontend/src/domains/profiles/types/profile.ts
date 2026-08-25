/** Сверено с backend: `Blizka.Api/Users/UserProfileResponse.cs`. */
export type UserProfile = {
  userId: string
  name: string
  age: number
  bio: string | null
  cityName: string
  photos: Array<{
    id: string
    url: string
    thumbnailUrl: string
    mediumUrl: string
    isMain: boolean
  }>
  interests: Array<{ id: string; name: string }>
  /** Ответы на вопросы анкеты; порядок задаёт сервер. */
  prompts: string[]
  isVerified: boolean
}
