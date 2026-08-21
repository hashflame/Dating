import { useNavigate } from '@tanstack/react-router'
import { MapPin } from 'lucide-react'
import { useCallback, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useCitySearch, useDetectCity, type City } from '@/domains/cities'
import { useSaveDraftStep } from '@/domains/onboarding'
import { ROUTES } from '@/shared/config'
import { useDebouncedValue } from '@/shared/hooks'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, Input, ListRow, Skeleton } from '@/shared/ui'

import { OnboardingStep } from './OnboardingStep'

/**
 * Шаг 3 (S-05): город — автоопределение по геолокации или поиск по каталогу.
 * Черновик здесь не подставляем: по сохранённому id название города не получить,
 * эндпоинта «город по id» нет — см. docs/api-gaps.md.
 */
export function CityPage() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const saveStep = useSaveDraftStep()
  const detect = useDetectCity()

  const [query, setQuery] = useState('')
  const [selected, setSelected] = useState<City | null>(null)
  const [detectFailed, setDetectFailed] = useState(false)

  const debouncedQuery = useDebouncedValue(query)
  const { data: cities, isFetching } = useCitySearch(debouncedQuery)

  // API отдаёт код страны («BY»), человеку нужно название на его языке.
  const countryNames = useMemo(
    () => new Intl.DisplayNames([i18n.language], { type: 'region' }),
    [i18n.language],
  )

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

  const showEmpty = debouncedQuery.length > 0 && !isFetching && cities?.length === 0

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

      {cities && cities.length > 0 && (
        <Card padding="none" className="overflow-hidden">
          {cities.map((city) => (
            <ListRow
              key={city.id}
              title={city.name}
              subtitle={countryNames.of(city.country) ?? city.country}
              selected={selected?.id === city.id}
              onClick={() => selectCity(city)}
            />
          ))}
        </Card>
      )}
    </OnboardingStep>
  )
}
