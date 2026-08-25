import { useNavigate } from '@tanstack/react-router'
import { BadgeCheck, Eye, Heart, Star } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useCity } from '@/domains/cities'
import { usePhotos } from '@/domains/photos'
import { useViewer, useViewerPreview } from '@/domains/viewer'
import { ROUTES } from '@/shared/config'
import { Card, ErrorState, ListRow, ProgressBar, Skeleton } from '@/shared/ui'
import { ProfileSheet } from '@/widgets/profile-sheet'

/**
 * Мой профиль (S-40).
 *
 * Собирается из трёх запросов, потому что сервер не отдаёт всё одним:
 * `GET /api/users/me` — поля и заполненность, `/photos` — фото,
 * `/cities/{id}` — название города по `cityId`. Возраст тоже считается здесь:
 * в ответе только `birthDate`.
 */
export function ProfilePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const viewer = useViewer()
  const photos = usePhotos()
  const city = useCity(viewer.data?.cityId ?? undefined)

  // Превью запрашиваем сразу, не по открытию шторки: из него берётся счётчик
  // интересов в списке — единственное место, где сервер их отдаёт.
  const preview = useViewerPreview(true)
  const [previewOpen, setPreviewOpen] = useState(false)

  if (viewer.isPending) return <ProfileSkeleton />
  if (viewer.isError) return <ErrorState onRetry={() => void viewer.refetch()} />

  const me = viewer.data
  const mainPhoto = (photos.data ?? []).find((photo) => photo.isMain) ?? photos.data?.[0]
  const interestsCount = preview.data?.interests.length

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-6">
      <section className="flex items-center gap-4">
        <span className="size-20 shrink-0 overflow-hidden rounded-xl bg-gradient-photo-1">
          {mainPhoto && <img src={mainPhoto.mediumUrl} alt="" className="size-full object-cover" />}
        </span>

        <span className="flex min-w-0 flex-col gap-0.5">
          <span className="flex items-center gap-1.5 text-display font-bold">
            <span className="truncate">
              {me.name}, {ageFromBirthDate(me.birthDate)}
            </span>
            {me.isVerified && <BadgeCheck className="size-5 shrink-0 text-brand" aria-hidden />}
          </span>

          <span className="truncate text-base text-muted-foreground">{city.data?.name ?? ''}</span>
        </span>
      </section>

      <Card padding="tight" className="flex flex-col gap-2">
        <span className="flex items-baseline justify-between gap-2">
          <span className="text-base font-semibold">{t('profile.completeness')}</span>
          <span className="text-tiny text-muted-foreground">{me.profileCompleteness}%</span>
        </span>

        <ProgressBar value={me.profileCompleteness} />

        {me.nextReward && (
          <span className="text-tiny text-muted-foreground">
            {t('profile.nextReward', {
              threshold: me.nextReward.threshold,
              reward: me.nextReward.sparksReward,
            })}
          </span>
        )}
      </Card>

      <Card padding="none" className="overflow-hidden">
        <ListRow
          title={t('profile.wallet')}
          subtitle={t('viewer.balance', { count: me.sparksBalance })}
          leading={<Star className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.profileWallet })}
        />

        <ListRow
          title={t('profile.interests')}
          subtitle={
            interestsCount === undefined
              ? t('profile.interestsHint')
              : t('profile.interestsCount', { count: interestsCount })
          }
          leading={<Heart className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.profileInterests })}
        />

        <ListRow
          title={t('profile.preview')}
          subtitle={t('profile.previewHint')}
          leading={<Eye className="size-5 text-brand" aria-hidden />}
          onClick={() => setPreviewOpen(true)}
        />
      </Card>

      <ProfileSheet
        profile={previewOpen ? (preview.data ?? null) : null}
        onClose={() => setPreviewOpen(false)}
        own
      />
    </main>
  )
}

/**
 * Возраст из даты рождения. Считаем на клиенте: `GET /api/users/me` отдаёт
 * только `birthDate`, а в ленте и превью возраст приходит уже посчитанным.
 */
function ageFromBirthDate(birthDate: string): number {
  const born = new Date(birthDate)
  const now = new Date()
  const years = now.getFullYear() - born.getFullYear()
  const hadBirthday =
    now.getMonth() > born.getMonth() ||
    (now.getMonth() === born.getMonth() && now.getDate() >= born.getDate())

  return hadBirthday ? years : years - 1
}

function ProfileSkeleton() {
  return (
    <main className="flex flex-col gap-4 px-4 pt-2">
      <div className="flex items-center gap-4">
        <Skeleton className="size-20 shrink-0 rounded-xl" />
        <Skeleton className="h-7 w-40" />
      </div>
      <Skeleton className="h-24 w-full rounded-md" />
      <Skeleton className="h-40 w-full rounded-md" />
    </main>
  )
}
