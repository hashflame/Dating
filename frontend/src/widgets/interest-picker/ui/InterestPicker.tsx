import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useInterestCatalog, useInterestSearch } from '@/domains/interests'
import { useDebouncedValue } from '@/shared/hooks'
import { useHaptic } from '@/shared/telegram'
import { Button, ErrorState, Input, Skeleton } from '@/shared/ui'
import { Tag } from '@/shared/ui/Tag'

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
        <div className="flex flex-wrap gap-1.5">
          {value.customInterests.map((name) => (
            <button key={name} type="button" onClick={() => removeCustom(name)}>
              <Tag highlighted>{name}</Tag>
            </button>
          ))}
        </div>
      )}

      {catalog.isPending && <Skeleton className="h-40 w-full rounded-md" />}
      {catalog.isError && <ErrorState onRetry={() => void catalog.refetch()} />}

      {searching ? (
        <InterestList
          interests={search.data ?? []}
          selectedIds={value.interestIds}
          onToggle={toggle}
          empty={search.isSuccess && search.data.length === 0}
        />
      ) : (
        catalog.data?.map((group) => (
          <section key={group.category} className="flex flex-col gap-1.5">
            <h3 className="text-tiny tracking-wide text-faint uppercase">
              {t(CATEGORY_KEYS[group.category])}
            </h3>

            <InterestList
              interests={group.interests}
              selectedIds={value.interestIds}
              onToggle={toggle}
              empty={false}
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
}

function InterestList({ interests, selectedIds, onToggle, empty }: InterestListProps) {
  const { t } = useTranslation()

  if (empty) {
    return <p className="text-base text-faint">{t('onboarding.interests.notFound')}</p>
  }

  return (
    <div className="flex flex-wrap gap-1.5">
      {interests.map((interest) => (
        <button key={interest.id} type="button" onClick={() => onToggle(interest.id)}>
          <Tag highlighted={selectedIds.includes(interest.id)}>{interest.name}</Tag>
        </button>
      ))}
    </div>
  )
}
