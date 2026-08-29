import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft, Ban, Download, PauseCircle, PlayCircle, Trash2 } from 'lucide-react'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useBlockedUsers } from '@/domains/moderation'
import {
  usePrivacySettings,
  useSavePrivacySettings,
  type PrivacySettingsPatch,
} from '@/domains/privacy'
import {
  useDeleteAccount,
  usePauseAccount,
  useRequestDataExport,
  useResumeAccount,
  useViewer,
} from '@/domains/viewer'
import { isApiError } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, ErrorState, ListRow, Skeleton } from '@/shared/ui'
import { SwitchRow } from '@/shared/ui/SwitchRow'

/**
 * Приватность (S-51): тумблеры видимости, список заблокированных и три
 * действия над аккаунтом — выгрузка данных, пауза и удаление.
 *
 * Удаление в два нажатия: восстановления в API нет, вход тем же Telegram-id
 * после него отдаёт 410, так что случайный тап стоит аккаунта.
 */
export function PrivacyPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()

  const settings = usePrivacySettings()
  const save = useSavePrivacySettings()
  const blocked = useBlockedUsers()
  const viewer = useViewer()

  const goBack = useCallback(() => void navigate({ to: ROUTES.profile }), [navigate])
  useBackButton(goBack)

  // Пока PATCH в полёте, показываем то, что человек только что переключил:
  // иначе тумблер отскакивал бы назад на время запроса.
  const current =
    settings.data && save.isPending ? { ...settings.data, ...save.variables } : settings.data

  const toggle = (patch: PrivacySettingsPatch): void => {
    haptic.select()
    save.mutate(patch)
  }

  const invisibleDenied = isApiError(save.error) && save.error.status === 422

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-safe-5">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('privacy.title')}</h1>
      </div>

      {settings.isPending && <Skeleton className="h-56 w-full rounded-md" />}
      {settings.isError && <ErrorState onRetry={() => void settings.refetch()} />}

      {current && (
        <>
          <Card padding="tight" className="flex flex-col gap-3">
            <SwitchRow
              label={t('privacy.blockIncoming')}
              hint={t('privacy.blockIncomingHint')}
              checked={current.blockIncomingMessages}
              onCheckedChange={(value) => toggle({ blockIncomingMessages: value })}
            />

            <SwitchRow
              label={t('privacy.invisible')}
              hint={t('privacy.invisibleHint')}
              checked={current.invisibleMode}
              onCheckedChange={(value) => toggle({ invisibleMode: value })}
            />

            <SwitchRow
              label={t('privacy.hideDistance')}
              hint={t('privacy.hideDistanceHint')}
              checked={current.hideDistance}
              onCheckedChange={(value) => toggle({ hideDistance: value })}
            />

            <SwitchRow
              label={t('privacy.hideAge')}
              checked={current.hideAge}
              onCheckedChange={(value) => toggle({ hideAge: value })}
            />
          </Card>

          {save.isError && (
            <p className="text-tiny text-destructive">
              {invisibleDenied ? t('privacy.invisibleDenied') : t('privacy.saveError')}
            </p>
          )}

          <Card padding="none" className="overflow-hidden">
            <ListRow
              title={t('privacy.blocked')}
              subtitle={
                blocked.data === undefined
                  ? undefined
                  : t('privacy.blockedCount', { count: blocked.data.length })
              }
              leading={<Ban className="size-5 text-brand" aria-hidden />}
              onClick={() => void navigate({ to: ROUTES.profileBlocked })}
            />
          </Card>

          <AccountActions paused={viewer.data?.status === 'paused'} />
        </>
      )}
    </main>
  )
}

type AccountActionsProps = {
  paused: boolean
}

/** Выгрузка данных, пауза и удаление — всё, что делает аккаунт менее видимым. */
function AccountActions({ paused }: AccountActionsProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()

  const exportData = useRequestDataExport()
  const pause = usePauseAccount()
  const resume = useResumeAccount()
  const deleteAccount = useDeleteAccount()
  const [confirming, setConfirming] = useState(false)

  const handleDelete = (): void => {
    haptic.tap()

    if (!confirming) {
      setConfirming(true)
      return
    }

    // Перезагрузка, а не переход: после удаления протух и токен, и вся
    // серверная картина мира — стартовый экран увидит 410 и объяснит это сам.
    deleteAccount.mutate(undefined, { onSuccess: () => window.location.reload() })
  }

  return (
    <>
      <Card padding="none" className="overflow-hidden">
        <ListRow
          title={t('privacy.dataExport')}
          subtitle={
            exportData.isSuccess ? t('privacy.dataExportQueued') : t('privacy.dataExportHint')
          }
          leading={<Download className="size-5 text-brand" aria-hidden />}
          onClick={() => {
            haptic.tap()
            exportData.mutate()
          }}
        />

        <ListRow
          title={paused ? t('privacy.resume') : t('privacy.pause')}
          subtitle={paused ? t('privacy.resumeHint') : t('privacy.pauseHint')}
          leading={
            paused ? (
              <PlayCircle className="size-5 text-brand" aria-hidden />
            ) : (
              <PauseCircle className="size-5 text-brand" aria-hidden />
            )
          }
          onClick={() => {
            haptic.tap()
            if (paused) resume.mutate()
            else pause.mutate()
          }}
        />

        <ListRow
          title={t('privacy.deleteAccount')}
          subtitle={confirming ? t('privacy.deleteConfirm') : t('privacy.deleteHint')}
          leading={<Trash2 className="size-5 text-destructive" aria-hidden />}
          onClick={handleDelete}
        />
      </Card>

      {(exportData.isError || pause.isError || resume.isError || deleteAccount.isError) && (
        <p className="text-tiny text-destructive">{t('privacy.actionError')}</p>
      )}
    </>
  )
}
