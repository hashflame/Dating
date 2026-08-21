/** Сверено с backend: `Blizka.Api/Photos/PhotoDtos.cs`. */
export type Photo = {
  id: string
  url: string
  thumbnailUrl: string
  mediumUrl: string
  sortOrder: number
  isMain: boolean
  createdAt: string
}

/** Лимит из backend: `UploadPhoto` отвечает 422 на седьмое фото. */
export const MAX_PHOTOS = 6
