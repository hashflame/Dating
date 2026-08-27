/**
 * «Анна, 24» — или просто «Анна», если возраст скрыт.
 *
 * Возраст приходит `null`, когда человек включил «Скрывать возраст» (T-16.1):
 * без этой проверки на карточке оставалась висеть запятая без числа.
 */
export function nameWithAge(name: string, age: number | null): string {
  return age === null ? name : `${name}, ${String(age)}`
}
