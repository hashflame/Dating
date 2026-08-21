/** Сверено с backend: `Blizka.Api/Cities/CityDtos.cs`. */
export type City = {
  id: string
  name: string
  /** ISO 3166-1 alpha-2, например `BY`. */
  country: string
  isOpen: boolean
}

/** Ответ `POST /api/geo/detect`. `city` пуст, если рядом нет каталожного города. */
export type DetectedCity = {
  city: City | null
  detectedAddress: string | null
}
