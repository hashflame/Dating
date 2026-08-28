import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft, Ban } from 'lucide-react'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import { useBlockedUsers, useUnblockUser } from '@/domains/moderation'
import { ROUTES } from '@/shared/config'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, EmptyState, ErrorState, ListRow, Skeleton } from '@/shared/ui'

/**
 * Заблокированные (S-51). Разблокировка возвращает человека в ленту обеим
 * сторонам, поэтому кнопка подписана действием, а не крестиком.
 */
export function BlockedPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()

  const blocked = useBlockedUsers()
  const unblock = useUnblockUser()

  const goBack = useCallback(() => void navigate({ to: ROUTES.profilePrivacy }), [navigate])
  useBackButton(goBack)

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-safe-5">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-heading text-display">{t('privacy.blocked')}</h1>
      </div>

      {blocked.isPending && <Skeleton className="h-40 w-full rounded-md" />}
      {blocked.isError && <ErrorState onRetry={() => void blocked.refetch()} />}

      {blocked.data?.length === 0 && <EmptyState icon={Ban} title={t('privacy.blockedEmpty')} />}

      {blocked.data && blocked.data.length > 0 && (
        <Card padding="none" className="overflow-hidden">
          {blocked.data.map((user) => (
            <ListRow
              key={user.userId}
              title={user.name}
              leading={
                <span className="size-10 shrink-0 overflow-hidden rounded-full bg-gradient-photo-1">
                  {user.mainPhotoUrl !== null && (
                    <img src={user.mainPhotoUrl} alt="" className="size-full object-cover" />
                  )}
                </span>
              }
              trailing={
                <span className="text-brand">
                  {unblock.isPending && unblock.variables === user.userId
                    ? t('privacy.unblocking')
                    : t('privacy.unblock')}
                </span>
              }
              onClick={() => {
                haptic.tap()
                unblock.mutate(user.userId)
              }}
            />
          ))}
        </Card>
      )}

      {unblock.isError && <p className="text-tiny text-destructive">{t('privacy.actionError')}</p>}
    </main>
  )
}
