import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useInterestCatalog, useInterestSearch } from '@/domains/interests'
import { useDebouncedValue } from '@/shared/hooks'
import { useHaptic } from '@/shared/telegram'
import { Button, ErrorState, Input, Skeleton } from '@/shared/ui'
import { ToggleGroup } from '@/shared/ui/kit/toggle-group'
import { OptionCard } from '@/shared/ui/OptionCard'

/** Типизированный `t()` не принимает шаблонную строку — держим ключи списком. */
const CATEGORY_KEYS = {
  sport: 'onboarding.interests.category.sport',
  creativity: 'onboarding.interests.category.creativity',
  entertainment: 'onboarding.interests.category.entertainment',
  foodAndDrinks: 'onboarding.interests.category.foodAndDrinks',
  growthAndTravel: 'onboarding.interests.category.growthAndTravel',
  custom: 'onboarding.interests.category.custom',
} as const

export type InterestSelection = {
  /** Id из каталога. */
  interestIds: string[]
  /** Названия, которых в каталоге нет — сервер создаёт их сам при сохранении. */
  customInterests: string[]
}

type InterestPickerProps = {
  value: InterestSelection
  onChange: (next: InterestSelection) => void
  max: number
}

/**
 * Выбор интересов (S-43): каталог по категориям, поиск и свои варианты.
 *
 * Один компонент на два места — шаг онбординга и настройки профиля: списки,
 * лимит и правило «нет в каталоге — добавь своё» там одинаковые, а различаются
 * только заголовок экрана и то, что происходит после сохранения.
 */
export function InterestPicker({ value, onChange, max }: InterestPickerProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()

  const catalog = useInterestCatalog()
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebouncedValue(query)
  const search = useInterestSearch(debouncedQuery)

  const total = value.interestIds.length + value.customInterests.length
  const canAddMore = total < max
  const searching = debouncedQuery.trim().length > 0

  const toggle = (id: string): void => {
    haptic.select()
    const selected = value.interestIds.includes(id)
    if (!selected && !canAddMore) return

    onChange({
      ...value,
      interestIds: selected
        ? value.interestIds.filter((item) => item !== id)
        : [...value.interestIds, id],
    })
  }

  const addCustom = (): void => {
    const name = query.trim()
    if (name === '' || !canAddMore || value.customInterests.includes(name)) return

    haptic.select()
    onChange({ ...value, customInterests: [...value.customInterests, name] })
    setQuery('')
  }

  const removeCustom = (name: string): void => {
    haptic.select()
    onChange({
      ...value,
      customInterests: value.customInterests.filter((item) => item !== name),
    })
  }

  return (
    <>
      <div className="flex gap-2">
        <Input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder={t('onboarding.interests.searchPlaceholder')}
          className="h-11"
        />

        {searching && (
          <Button variant="secondary" size="lg" disabled={!canAddMore} onClick={addCustom}>
            <Plus aria-hidden />
            {t('onboarding.interests.add')}
          </Button>
        )}
      </div>

      {value.customInterests.length > 0 && (
        <ToggleGroup
          type="multiple"
          value={value.customInterests}
          onValueChange={(next: string[]) => {
            const removed = value.customInterests.find((name) => !next.includes(name))
            if (removed !== undefined) removeCustom(removed)
          }}
          aria-label={t('onboarding.interests.category.custom')}
          spacing={2}
          className="grid w-full grid-cols-3"
        >
          {value.customInterests.map((name) => (
            <OptionCard key={name} value={name} label={name} />
          ))}
        </ToggleGroup>
      )}

      {catalog.isPending && <Skeleton className="h-40 w-full rounded-md" />}
      {catalog.isError && <ErrorState onRetry={() => void catalog.refetch()} />}

      {searching ? (
        <InterestList
          interests={search.data ?? []}
          selectedIds={value.interestIds}
          onToggle={toggle}
          empty={search.isSuccess && search.data.length === 0}
          label={t('onboarding.interests.searchPlaceholder')}
        />
      ) : (
        catalog.data?.map((group) => (
          <section key={group.category} className="flex flex-col gap-1.5">
            <h3 className="text-eyebrow font-bold text-muted-foreground uppercase">
              {t(CATEGORY_KEYS[group.category])}
            </h3>

            <InterestList
              interests={group.interests}
              selectedIds={value.interestIds}
              onToggle={toggle}
              empty={false}
              label={t(CATEGORY_KEYS[group.category])}
            />
          </section>
        ))
      )}

      <p className="text-tiny text-muted-foreground">
        {t('onboarding.interests.counter', { count: total, max })}
      </p>
    </>
  )
}

type InterestListProps = {
  interests: ReadonlyArray<{ id: string; name: string }>
  selectedIds: readonly string[]
  onToggle: (id: string) => void
  /** Поиск ничего не нашёл — предлагаем добавить своё. */
  empty: boolean
  /** Название группы для читалок. */
  label: string
}

/**
 * Список интересов одной категории — теми же карточками, что и цели
 * знакомства: заливка, фирменный цвет выбранного, галочка в углу. Раньше
 * это были мелкие «пилюли», и выбор из полусотни вариантов приходилось
 * вычитывать по цвету текста.
 *
 * Три колонки: у интересов короткие названия, и в две они растягивались бы
 * на пол-экрана, а список из шести категорий стал бы бесконечным.
 */
function InterestList({ interests, selectedIds, onToggle, empty, label }: InterestListProps) {
  const { t } = useTranslation()

  if (empty) {
    return <p className="text-base text-faint">{t('onboarding.interests.notFound')}</p>
  }

  // Радикс отдаёт весь набор нажатого внутри группы, а лимит и свои интересы
  // считаются снаружи по одному — поэтому вычисляем изменившийся элемент.
  const pressed = interests
    .filter((interest) => selectedIds.includes(interest.id))
    .map((interest) => interest.id)

  return (
    <ToggleGroup
      type="multiple"
      value={pressed}
      onValueChange={(next: string[]) => {
        const changed =
          next.length > pressed.length
            ? next.find((id) => !pressed.includes(id))
            : pressed.find((id) => !next.includes(id))

        if (changed !== undefined) onToggle(changed)
      }}
      aria-label={label}
      spacing={2}
      className="grid w-full grid-cols-3"
    >
      {interests.map((interest) => (
        <OptionCard key={interest.id} value={interest.id} label={interest.name} />
      ))}
    </ToggleGroup>
  )
}
