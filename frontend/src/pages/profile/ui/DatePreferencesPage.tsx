import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft } from 'lucide-react'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  useDatePreferenceCatalog,
  useSaveDatePreferences,
  type DatePreferenceCode,
} from '@/domains/date-preferences'
import { ROUTES } from '@/shared/config'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, ErrorState, Skeleton, ListRow } from '@/shared/ui'

/**
 * Предпочтения на свидания (S-42). Заполняются один раз и участвуют в подборе
 * ленты и в идеях свидания.
 *
 * Экран открывается с пустым выбором, а не с сохранённым: прочитать текущий
 * набор нечем — `GET /api/users/me/date-preferences` отдаёт 405, в анкете их
 * тоже нет (docs/api-gaps.md). Поэтому под кнопкой честно сказано, что
 * сохранение заменяет прежний выбор целиком.
 */
export function DatePreferencesPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()

  const catalog = useDatePreferenceCatalog()
  const save = useSaveDatePreferences()
  const [selected, setSelected] = useState<DatePreferenceCode[]>([])

  const goBack = useCallback(() => void navigate({ to: ROUTES.profile }), [navigate])
  useBackButton(goBack)

  const toggle = (code: DatePreferenceCode): void => {
    haptic.select()
    setSelected((current) =>
      current.includes(code) ? current.filter((item) => item !== code) : [...current, code],
    )
  }

  const handleSave = (): void => {
    haptic.tap()
    save.mutate(selected, {
      onSuccess: () => {
        haptic.success()
        goBack()
      },
    })
  }

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('profile.datePrefs')}</h1>
      </div>

      <p className="text-base text-muted-foreground">{t('profile.datePrefsHint')}</p>

      {catalog.isPending && <Skeleton className="h-40 w-full rounded-md" />}
      {catalog.isError && <ErrorState onRetry={() => void catalog.refetch()} />}

      {catalog.data && (
        <>
          <div className="overflow-hidden rounded-md bg-surface">
            {catalog.data.map((preference) => (
              <ListRow
                key={preference.id}
                title={preference.name}
                selected={selected.includes(preference.code)}
                onClick={() => toggle(preference.code)}
              />
            ))}
          </div>

          <p className="text-tiny text-faint">{t('profile.datePrefsReplaceWarning')}</p>

          {save.isError && (
            <p className="text-center text-tiny text-destructive">{t('onboarding.saveError')}</p>
          )}

          <Button size="lg" block disabled={save.isPending} onClick={handleSave}>
            {t('action.save')}
          </Button>
        </>
      )}
    </main>
  )
}
