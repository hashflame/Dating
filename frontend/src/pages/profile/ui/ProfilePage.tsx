import { useNavigate } from '@tanstack/react-router'
import {
  BadgeCheck,
  CalendarHeart,
  Eye,
  Heart,
  Images,
  Lightbulb,
  Shield,
  SquarePen,
  Star,
  UserPlus,
} from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useViewer, useViewerPreview } from '@/domains/viewer'
import { ROUTES } from '@/shared/config'
import { nameWithAge } from '@/shared/lib'
import { Card, ErrorState, ListRow, Skeleton } from '@/shared/ui'
import { ProfileSheet } from '@/widgets/profile-sheet'

import { ProfileCompleteness } from './ProfileCompleteness'

/**
 * Мой профиль (S-40).
 *
 * Одного `GET /api/users/me` хватает на весь экран: возраст, название города,
 * фото и интересы сервер считает и отдаёт сам (фикс T-9.1). Превью
 * подгружается только для шторки «как видят другие».
 */
export function ProfilePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const viewer = useViewer()
  const [previewOpen, setPreviewOpen] = useState(false)
  const preview = useViewerPreview(previewOpen)

  if (viewer.isPending) return <ProfileSkeleton />
  if (viewer.isError) return <ErrorState onRetry={() => void viewer.refetch()} />

  const me = viewer.data
  const mainPhoto = me.photos.find((photo) => photo.isMain) ?? me.photos[0]

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-6">
      <section className="flex items-center gap-4">
        <span className="size-20 shrink-0 overflow-hidden rounded-lg bg-gradient-photo-1">
          {mainPhoto && <img src={mainPhoto.mediumUrl} alt="" className="size-full object-cover" />}
        </span>

        <span className="flex min-w-0 flex-col gap-0.5">
          <span className="flex items-center gap-1.5 text-display font-bold">
            <span className="truncate">{nameWithAge(me.name, me.age)}</span>
            {me.isVerified && <BadgeCheck className="size-5 shrink-0 text-brand" aria-hidden />}
          </span>

          <span className="truncate text-base text-muted-foreground">{me.cityName}</span>
        </span>
      </section>

      <ProfileCompleteness viewer={me} />

      <Card padding="none" className="overflow-hidden">
        <ListRow
          title={t('profile.edit.title')}
          subtitle={t('profile.editHint')}
          leading={<SquarePen className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.profileEdit })}
        />

        <ListRow
          title={t('profile.photos')}
          subtitle={t('profile.photosCount', { count: me.photos.length })}
          leading={<Images className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.profilePhotos })}
        />

        <ListRow
          title={t('profile.wallet')}
          subtitle={t('viewer.balance', { count: me.sparksBalance })}
          leading={<Star className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.profileWallet })}
        />

        <ListRow
          title={t('profile.interests')}
          subtitle={t('profile.interestsCount', { count: me.interests.length })}
          leading={<Heart className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.profileInterests })}
        />

        <ListRow
          title={t('profile.datePrefs')}
          subtitle={t('profile.datePrefsShort')}
          leading={<CalendarHeart className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.profileDatePrefs })}
        />

        <ListRow
          title={t('profile.preview')}
          subtitle={t('profile.previewHint')}
          leading={<Eye className="size-5 text-brand" aria-hidden />}
          onClick={() => setPreviewOpen(true)}
        />
      </Card>

      <Card padding="none" className="overflow-hidden">
        <ListRow
          title={t('invite.title')}
          subtitle={t('invite.rowHint')}
          leading={<UserPlus className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.profileInvite })}
        />

        <ListRow
          title={t('tabs.ideas')}
          subtitle={t('profile.ideasHint')}
          leading={<Lightbulb className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.ideas })}
        />

        <ListRow
          title={t('privacy.title')}
          subtitle={t('privacy.rowHint')}
          leading={<Shield className="size-5 text-brand" aria-hidden />}
          onClick={() => void navigate({ to: ROUTES.profilePrivacy })}
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

function ProfileSkeleton() {
  return (
    <main className="flex flex-col gap-4 px-4 pt-2">
      <div className="flex items-center gap-4">
        <Skeleton className="size-20 shrink-0 rounded-lg" />
        <Skeleton className="h-7 w-40" />
      </div>
      <Skeleton className="h-24 w-full rounded-md" />
      <Skeleton className="h-40 w-full rounded-md" />
    </main>
  )
}
