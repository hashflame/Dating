import { useNavigate } from '@tanstack/react-router'
import { MapPin } from 'lucide-react'
import { useCallback, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useCity, useCitySearch, useDetectCity, type City } from '@/domains/cities'
import { useOnboardingDraft, useSaveDraftStep } from '@/domains/onboarding'
import { ROUTES } from '@/shared/config'
import { useDebouncedValue } from '@/shared/hooks'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, ErrorState, Input, ListRow, Skeleton } from '@/shared/ui'

import { OnboardingStep } from './OnboardingStep'
import { OnboardingStepSkeleton } from './OnboardingStepSkeleton'

/**
 * Шаг 3 (S-05): город — автоопределение по геолокации или поиск по каталогу.
 * Сохранённый выбор восстанавливаем: в черновике лежит только `cityId`,
 * название получаем через `GET /api/cities/{cityId}`.
 */
export function CityPage() {
  const draft = useOnboardingDraft()
  const savedCity = useCity(draft.data?.data.cityId)

  if (draft.isPending) return <OnboardingStepSkeleton />
  if (draft.isError) return <ErrorState onRetry={() => void draft.refetch()} />

  // Название сохранённого города грузится отдельным запросом: пока он идёт,
  // держим скелетон, иначе список мигнёт пустым и выбор будто сбросился.
  if (savedCity.isLoading) return <OnboardingStepSkeleton />

  return <CityForm defaultCity={savedCity.data ?? null} />
}

type CityFormProps = {
  defaultCity: City | null
}

function CityForm({ defaultCity }: CityFormProps) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const saveStep = useSaveDraftStep()
  const detect = useDetectCity()

  const [query, setQuery] = useState(defaultCity?.name ?? '')
  const [selected, setSelected] = useState<City | null>(defaultCity)
  const [detectFailed, setDetectFailed] = useState(false)

  const debouncedQuery = useDebouncedValue(query)
  // Пока город выбран, поиск не нужен: в поле лежит его название, и запрос
  // вернул бы «похожие» — Пинск и Смоленск на «Минск». Список ждёт правки ввода.
  const { data: cities, isFetching } = useCitySearch(selected === null ? debouncedQuery : '')

  // Подпись под названием: область («Витебская область») или страна для
  // диаспоры («Литва») — их API отдаёт готовыми в `region`. Если региона нет,
  // остаётся код страны («BY»), и его нужно превратить в название на языке
  // интерфейса.
  const countryNames = useMemo(
    () => new Intl.DisplayNames([i18n.language], { type: 'region' }),
    [i18n.language],
  )

  const describeCity = (city: City): string =>
    city.region ?? countryNames.of(city.country) ?? city.country

  const goBack = useCallback(() => void navigate({ to: ROUTES.onboardingPreferences }), [navigate])
  useBackButton(goBack)

  const selectCity = (city: City): void => {
    haptic.select()
    setSelected(city)
    setQuery(city.name)
  }

  const handleDetect = (): void => {
    haptic.tap()
    setDetectFailed(false)

    if (!navigator.geolocation) {
      setDetectFailed(true)
      return
    }

    navigator.geolocation.getCurrentPosition(
      ({ coords }) =>
        detect.mutate(
          { lat: coords.latitude, lon: coords.longitude },
          {
            onSuccess: (result) => {
              if (!result.city) {
                setDetectFailed(true)
                return
              }

              haptic.success()
              selectCity(result.city)
            },
            onError: () => setDetectFailed(true),
          },
        ),
      () => setDetectFailed(true),
    )
  }

  const handleNext = (): void => {
    if (!selected) return

    haptic.tap()
    saveStep.mutate(
      { step: 3, data: { cityId: selected.id } },
      { onSuccess: () => void navigate({ to: ROUTES.onboardingPhotos }) },
    )
  }

  const searching = selected === null
  const showEmpty = searching && debouncedQuery.length > 0 && !isFetching && cities?.length === 0

  return (
    <OnboardingStep
      step={3}
      title={t('onboarding.city.title')}
      description={t('onboarding.city.description')}
      actionLabel={t('action.next')}
      onAction={handleNext}
      onBack={goBack}
      actionDisabled={!selected || saveStep.isPending}
      error={saveStep.isError ? t('onboarding.saveError') : undefined}
    >
      <Button
        variant="secondary"
        size="lg"
        block
        onClick={handleDetect}
        disabled={detect.isPending}
      >
        <MapPin aria-hidden />
        {detect.isPending ? t('onboarding.city.detecting') : t('onboarding.city.detect')}
      </Button>

      {detectFailed && (
        <p className="text-tiny text-destructive">{t('onboarding.city.detectFailed')}</p>
      )}

      <Input
        value={query}
        onChange={(event) => {
          setQuery(event.target.value)
          setSelected(null)
        }}
        placeholder={t('onboarding.city.searchPlaceholder')}
        className="h-11"
      />

      {isFetching && <Skeleton className="h-11 w-full" />}

      {showEmpty && (
        <p className="text-tiny text-muted-foreground">{t('onboarding.city.notFound')}</p>
      )}

      {/* Выбранный город — одной строкой вместо списка: список тут уже не нужен,
          а строка подтверждает выбор и показывает область или страну. */}
      {selected !== null && (
        <Card padding="none" className="overflow-hidden">
          <ListRow title={selected.name} subtitle={describeCity(selected)} selected />
        </Card>
      )}

      {searching && cities && cities.length > 0 && (
        <Card padding="none" className="overflow-hidden">
          {cities.map((city) => (
            <ListRow
              key={city.id}
              title={city.name}
              subtitle={describeCity(city)}
              onClick={() => selectCity(city)}
            />
          ))}
        </Card>
      )}
    </OnboardingStep>
  )
}
