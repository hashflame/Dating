import { useNavigate } from '@tanstack/react-router'
import {
  BadgeCheck,
  ChevronRight,
  Images,
  Lightbulb,
  Shield,
  SquarePen,
  UserPlus,
} from 'lucide-react'
import { type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { useViewer } from '@/domains/viewer'
import { ROUTES } from '@/shared/config'
import { nameWithAge } from '@/shared/lib'
import { Card, ErrorState, ListRow, Skeleton, SparkIcon } from '@/shared/ui'
import { MessageLimitsCard } from '@/widgets/message-limits'

import { ProfileCompleteness } from './ProfileCompleteness'

/**
 * Мой профиль (S-40).
 *
 * Одного `GET /api/users/me` хватает на весь экран: возраст, название города,
 * фото и интересы сервер считает и отдаёт сам (фикс T-9.1).
 *
 * Порядок экрана: кто я — кошелёк с балансом, ради которого сюда чаще всего и
 * заходят, поэтому он первым — потом заполненность карточки и сразу за ней
 * раздел «карточка» (это про одно и то же, они должны идти подряд) — потом
 * лимиты сообщений — и только потом раздел «приложение».
 *
 * Разделов осталось два — «карточка» и «приложение». Всё, что правит саму
 * анкету (интересы, предпочтения, «как видят другие»), живёт на экране
 * «Редактировать карточку»: это части анкеты, а не разделы профиля.
 */
export function ProfilePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const viewer = useViewer()

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

      {/* Кошелёк — не строка списка: баланс показан цифрой, ради которой сюда
          и заходят, а не спрятан в подпись. Первым на экране — это то, за
          чем чаще всего возвращаются. */}
      <button
        type="button"
        onClick={() => void navigate({ to: ROUTES.profileWallet })}
        className="flex items-center gap-3 rounded-lg bg-surface p-4 text-left transition-colors duration-150 outline-none hover:bg-surface-strong focus-visible:bg-surface-strong"
      >
        <span
          className="flex size-11 shrink-0 items-center justify-center rounded-full bg-spark/15"
          aria-hidden
        >
          <SparkIcon className="size-5" />
        </span>

        <span className="flex min-w-0 flex-1 flex-col">
          <span className="truncate text-base font-semibold">{t('profile.wallet')}</span>
          <span className="truncate text-tiny text-faint">{t('profile.walletHint')}</span>
        </span>

        <span className="flex shrink-0 items-center gap-1">
          <span className="text-display font-bold">{me.sparksBalance}</span>
          <ChevronRight className="size-4 text-faint" aria-hidden />
        </span>
      </button>

      {/* Заполненность и раздел «карточка» — про одно и то же, поэтому идут
          подряд. */}
      <ProfileCompleteness viewer={me} />

      <Section title={t('profile.sectionCard')}>
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
      </Section>

      {/* Лимиты сообщений: сколько можно написать бесплатно и чем платить,
          когда бесплатные кончились. */}
      <section className="flex flex-col gap-2">
        <h2 className="px-1 text-eyebrow font-bold text-muted-foreground uppercase">
          {t('messages.limits.title')}
        </h2>

        <MessageLimitsCard />
      </section>

      <Section title={t('profile.sectionApp')}>
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
      </Section>
    </main>
  )
}

type SectionProps = {
  title: string
  children: ReactNode
}

/** Подписанная группа строк: рубрика капсом и карточка со списком под ней. */
function Section({ title, children }: SectionProps) {
  return (
    <section className="flex flex-col gap-2">
      <h2 className="px-1 text-eyebrow font-bold text-muted-foreground uppercase">{title}</h2>

      <Card padding="none" className="overflow-hidden">
        {children}
      </Card>
    </section>
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
