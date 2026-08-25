/** Сверено с backend: `Blizka.Api/Interests/InterestDtos.cs`. */
export type Interest = {
  id: string
  name: string
  /** Создан пользователями, а не входит в стартовый каталог. */
  isCustom: boolean
}

/** Сверено с backend: `Blizka.App/Domain/Enums/InterestCategory.cs`. */
export type InterestCategory =
  'sport' | 'creativity' | 'entertainment' | 'foodAndDrinks' | 'growthAndTravel' | 'custom'

export type InterestGroup = {
  category: InterestCategory
  interests: Interest[]
}
