import { useNavigate } from '@tanstack/react-router'
import { Plus } from 'lucide-react'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useInterestCatalog, useInterestSearch, useSaveInterests } from '@/domains/interests'
import { useCompleteOnboarding } from '@/domains/onboarding'
import { isApiError } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { useDebouncedValue } from '@/shared/hooks'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, ErrorState, Input, Skeleton } from '@/shared/ui'
import { Tag } from '@/shared/ui/Tag'

import { OnboardingStep } from './OnboardingStep'

/** Продуктовое правило: минимум три интереса, иначе подбор не за что зацепить. */
const MIN_INTERESTS = 3
const MAX_INTERESTS = 12

/**
 * Шаг 5 (S-09): интересы. Завершает онбординг.
 *
 * Выбранное храним двумя множествами: id из каталога и названия, которых там
 * нет — их сервер создаёт сам при сохранении (`customInterests`). Иначе для
 * своего интереса пришлось бы сначала делать отдельный запрос на создание.
 */
export function InterestsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()

  const catalog = useInterestCatalog()
  const save = useSaveInterests()
  const complete = useCompleteOnboarding()

  const [query, setQuery] = useState('')
  const debouncedQuery = useDebouncedValue(query)
  const search = useInterestSearch(debouncedQuery)

  const [selected, setSelected] = useState<Map<string, string>>(new Map())
  const [custom, setCustom] = useState<string[]>([])

  const goBack = useCallback(() => void navigate({ to: ROUTES.onboardingPhotos }), [navigate])
  useBackButton(goBack)

  const total = selected.size + custom.length
  const canAddMore = total < MAX_INTERESTS

  const toggle = (id: string, name: string): void => {
    haptic.select()
    setSelected((current) => {
      const next = new Map(current)
      if (next.has(id)) next.delete(id)
      else if (canAddMore) next.set(id, name)

      return next
    })
  }

  const addCustom = (): void => {
    const name = query.trim()
    if (name === '' || !canAddMore) return

    haptic.select()
    setCustom((current) => (current.includes(name) ? current : [...current, name]))
    setQuery('')
  }

  const removeCustom = (name: string): void => {
    haptic.select()
    setCustom((current) => current.filter((item) => item !== name))
  }

  /** Сохраняем интересы и сразу завершаем анкету — это последний шаг. */
  const handleFinish = (): void => {
    haptic.tap()
    save.mutate(
      { interestIds: [...selected.keys()], customInterests: custom },
      {
        onSuccess: () =>
          complete.mutate(undefined, {
            onSuccess: () => {
              haptic.success()
              void navigate({ to: ROUTES.onboardingDone })
            },
            onError: (error) => {
              // Анкета уже завершена (409) — вести на «не удалось» неверно.
              if (isApiError(error) && error.code === 'ONBOARDING_ALREADY_COMPLETED') {
                void navigate({ to: ROUTES.feed, replace: true })
                return
              }

              haptic.error()
            },
          }),
      },
    )
  }

  const searching = debouncedQuery.trim().length > 0
  const isBusy = save.isPending || complete.isPending

  return (
    <OnboardingStep
      step={5}
      title={t('onboarding.interests.title')}
      description={t('onboarding.interests.description', { min: MIN_INTERESTS })}
      actionLabel={t('action.done')}
      onAction={handleFinish}
      onBack={goBack}
      actionDisabled={total < MIN_INTERESTS || isBusy}
      error={save.isError || complete.isError ? t('onboarding.saveError') : undefined}
    >
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

      {custom.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {custom.map((name) => (
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
          selected={selected}
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
              selected={selected}
              onToggle={toggle}
              empty={false}
            />
          </section>
        ))
      )}

      <p className="text-tiny text-muted-foreground">
        {t('onboarding.interests.counter', { count: total, max: MAX_INTERESTS })}
      </p>
    </OnboardingStep>
  )
}

/** Типизированный `t()` не принимает шаблонную строку — держим ключи списком. */
const CATEGORY_KEYS = {
  sport: 'onboarding.interests.category.sport',
  creativity: 'onboarding.interests.category.creativity',
  entertainment: 'onboarding.interests.category.entertainment',
  foodAndDrinks: 'onboarding.interests.category.foodAndDrinks',
  growthAndTravel: 'onboarding.interests.category.growthAndTravel',
  custom: 'onboarding.interests.category.custom',
} as const

type InterestListProps = {
  interests: ReadonlyArray<{ id: string; name: string }>
  selected: Map<string, string>
  onToggle: (id: string, name: string) => void
  /** Поиск ничего не нашёл — предлагаем добавить своё. */
  empty: boolean
}

function InterestList({ interests, selected, onToggle, empty }: InterestListProps) {
  const { t } = useTranslation()

  if (empty) {
    return <p className="text-base text-faint">{t('onboarding.interests.notFound')}</p>
  }

  return (
    <div className="flex flex-wrap gap-1.5">
      {interests.map((interest) => (
        <button
          key={interest.id}
          type="button"
          onClick={() => onToggle(interest.id, interest.name)}
        >
          <Tag highlighted={selected.has(interest.id)}>{interest.name}</Tag>
        </button>
      ))}
    </div>
  )
}
