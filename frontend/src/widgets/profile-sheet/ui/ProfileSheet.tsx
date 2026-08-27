import { BadgeCheck, Ban, Flag, Sparkles, Target, Users } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import {
  REPORT_REASONS,
  useBlockUser,
  useReportUser,
  type ReportReason,
} from '@/domains/moderation'
import { distanceInKm, nameWithAge } from '@/shared/lib'
import { Button, Card, Checkbox, ListRow } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { Textarea } from '@/shared/ui/kit/textarea'
import { Tag } from '@/shared/ui/Tag'

import { describeActivity } from '../lib/describe-activity'

/** Типизированный `t()` не принимает шаблонную строку — держим ключи списком. */
const ACTIVITY_KEYS = {
  today: 'feed.lastActive.today',
  week: 'feed.lastActive.week',
  long: 'feed.lastActive.long',
} as const

/**
 * Что нужно шторке от анкеты.
 *
 * Поля со `?` есть только у карточек ленты: в списке симпатий сервер не считает
 * ни совместимость, ни расстояние, ни активность. Секции под них тогда просто
 * не показываются — писать «пока не заполнено» там было бы неправдой, данные
 * есть, их не считают для этого экрана.
 */
export type ProfileDetails = {
  userId: string
  name: string
  /** `null` — возраст скрыт настройками приватности (T-16.1). */
  age: number | null
  bio: string | null
  cityName: string
  interests: ReadonlyArray<{ id: string; name: string; isMatch?: boolean }>
  prompts: readonly string[]
  isVerified: boolean
  distanceKm?: number | null
  compatibilityScore?: number
  compatibilitySummary?: {
    datingGoalMatch: boolean
    sharedInterestsCount: number
    bothVerified: boolean
  }
  lastActive?: string | null
}

type ProfileSheetProps = {
  /** `null` — шторка закрыта. Анкету держим снаружи, чтобы не терять анимацию закрытия. */
  profile: ProfileDetails | null
  onClose: () => void
  /** Своя анкета в режиме «как видят другие»: блокировка и жалоба тут не к месту. */
  own?: boolean
}

/**
 * Полная анкета (S-11). Открывается кнопкой с карточки ленты и тапом по
 * человеку в симпатиях.
 *
 * Фото здесь нет намеренно: их только что смотрели на карточке, а шторка нужна
 * ради текста — совпадений, интересов, ценностей и ответов на вопросы.
 */
export function ProfileSheet({ profile, onClose, own = false }: ProfileSheetProps) {
  const { t } = useTranslation()

  return (
    <Sheet open={profile !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex max-h-[92vh] flex-col gap-0 overflow-hidden rounded-t-xl border-0 p-0"
      >
        {profile && <ProfileBody profile={profile} onClose={onClose} own={own} />}
      </SheetContent>
    </Sheet>
  )
}

type ProfileBodyProps = {
  profile: ProfileDetails
  onClose: () => void
  own: boolean
}

/**
 * Отдельный компонент, а не тело шторки: состояние жалобы должно сбрасываться
 * при смене анкеты, а шторка остаётся смонтированной ради анимации закрытия.
 */
function ProfileBody({ profile, onClose, own }: ProfileBodyProps) {
  const { t } = useTranslation()

  const km = distanceInKm(profile.distanceKm)
  const place =
    km === null ? profile.cityName : t('feed.cityWithDistance', { city: profile.cityName, km })
  const activity = describeActivity(profile.lastActive ?? null)
  const summary = profile.compatibilitySummary

  return (
    <div className="flex min-h-0 flex-col gap-4 overflow-y-auto px-5 pt-5 pb-safe-5">
      <div className="flex flex-col gap-1">
        <SheetTitle className="flex items-center gap-2 text-display font-bold">
          {nameWithAge(profile.name, profile.age)}
          {profile.isVerified && <BadgeCheck className="size-5 text-brand" aria-hidden />}
        </SheetTitle>

        <SheetDescription className="text-base">{place}</SheetDescription>
      </div>

      {profile.compatibilityScore !== undefined && (
        <Card padding="tight" className="flex flex-col gap-2">
          <span className="flex items-center gap-2 text-base font-semibold">
            <Sparkles className="size-4 text-brand" aria-hidden />
            {t('feed.compatibility', { score: profile.compatibilityScore })}
          </span>

          {summary && (
            <span className="flex flex-wrap gap-1.5">
              {summary.datingGoalMatch && (
                <Tag highlighted>
                  <Target className="size-3" aria-hidden />
                  {t('feed.summary.sameGoal')}
                </Tag>
              )}
              {summary.sharedInterestsCount > 0 && (
                <Tag highlighted>
                  <Users className="size-3" aria-hidden />
                  {t('feed.summary.sharedInterests', { count: summary.sharedInterestsCount })}
                </Tag>
              )}
              {summary.bothVerified && (
                <Tag highlighted>
                  <BadgeCheck className="size-3" aria-hidden />
                  {t('feed.summary.bothVerified')}
                </Tag>
              )}
            </span>
          )}
        </Card>
      )}

      {/* Секции показываем всегда: пустая говорит о человеке не меньше,
          чем заполненная, а исчезающие блоки выглядят как недогруз. */}
      <Section title={t('feed.section.about')} empty={profile.bio === null}>
        {profile.bio}
      </Section>

      <Section title={t('feed.section.interests')} empty={profile.interests.length === 0}>
        <span className="flex flex-wrap gap-1.5">
          {profile.interests.map((interest) => (
            <Tag key={interest.id} highlighted={interest.isMatch === true}>
              {interest.name}
            </Tag>
          ))}
        </span>
      </Section>

      {/* Ценности и предпочтения на свидания есть в спеке (S-11), но ни лента,
          ни анкета их пока не отдают — см. docs/api-gaps.md. */}
      <Section title={t('feed.section.values')} empty>
        {null}
      </Section>

      <Section title={t('feed.section.datePrefs')} empty>
        {null}
      </Section>

      <Section title={t('feed.section.prompts')} empty={profile.prompts.length === 0}>
        <span className="flex flex-col gap-3">
          {profile.prompts.map((prompt, index) => (
            <span key={index} className="block">
              {prompt}
            </span>
          ))}
        </span>
      </Section>

      {activity !== null && (
        <Section title={t('feed.section.activity')} empty={false}>
          {t(ACTIVITY_KEYS[activity])}
        </Section>
      )}

      {!own && <SafetyActions userId={profile.userId} onBlocked={onClose} />}
    </div>
  )
}

type SafetyActionsProps = {
  userId: string
  onBlocked: () => void
}

/** Блокировка и жалоба (S-11, S-13). Внизу анкеты — их ищут, когда уже решили. */
function SafetyActions({ userId, onBlocked }: SafetyActionsProps) {
  const { t } = useTranslation()
  const block = useBlockUser()
  const report = useReportUser()
  const [reporting, setReporting] = useState(false)

  if (block.isSuccess) {
    return <p className="text-center text-tiny text-muted-foreground">{t('feed.safety.blocked')}</p>
  }

  if (report.isSuccess) {
    return (
      <p className="text-center text-tiny text-muted-foreground">{t('feed.safety.reported')}</p>
    )
  }

  return (
    <section className="flex flex-col gap-2 border-t border-border pt-4">
      <h3 className="text-tiny tracking-wide text-faint uppercase">{t('feed.safety.title')}</h3>

      {reporting ? (
        <ReportForm
          pending={report.isPending}
          onSubmit={(reason, comment, blockUser) =>
            report.mutate({ userId, reason, comment, blockUser })
          }
        />
      ) : (
        <div className="flex gap-2">
          <Button
            variant="secondary"
            size="sm"
            className="flex-1"
            disabled={block.isPending}
            onClick={() => block.mutate(userId, { onSuccess: onBlocked })}
          >
            <Ban aria-hidden />
            {t('feed.safety.block')}
          </Button>

          <Button
            variant="secondary"
            size="sm"
            className="flex-1"
            onClick={() => setReporting(true)}
          >
            <Flag aria-hidden />
            {t('feed.safety.report')}
          </Button>
        </div>
      )}

      {(block.isError || report.isError) && (
        <p className="text-tiny text-destructive">{t('feed.safety.error')}</p>
      )}
    </section>
  )
}

type ReportFormProps = {
  pending: boolean
  onSubmit: (reason: ReportReason, comment: string, blockUser: boolean) => void
}

/**
 * Форма жалобы (S-13): причина, необязательный комментарий и отдельное решение
 * «заблокировать заодно» — сервер принимает его тем же запросом (T-17.1).
 *
 * Причина одна: критичные (`underage`, `unsafeMeeting`) уводят аккаунт в
 * блокировку немедленно, поэтому смешивать их с остальными в одном отчёте
 * нельзя — модератор должен видеть, на что именно жалуются.
 *
 * Шторку после отправки не закрываем, даже когда заодно ставится блокировка:
 * подтверждение «проверим в течение 12 часов» — это то, ради чего человек и
 * жаловался. Закрыть он может сам.
 */
function ReportForm({ pending, onSubmit }: ReportFormProps) {
  const { t } = useTranslation()
  const [reason, setReason] = useState<ReportReason | null>(null)
  const [comment, setComment] = useState('')
  const [blockUser, setBlockUser] = useState(true)

  return (
    <div className="flex flex-col gap-3">
      <div className="overflow-hidden rounded-xl border border-border">
        {REPORT_REASONS.map((item) => (
          <ListRow
            key={item.value}
            title={t(item.labelKey)}
            selected={reason === item.value}
            onClick={() => setReason(item.value)}
          />
        ))}
      </div>

      <Textarea
        value={comment}
        onChange={(event) => setComment(event.target.value)}
        placeholder={t('feed.safety.commentPlaceholder')}
        maxLength={1000}
        rows={3}
      />

      <label className="flex items-center gap-2 text-base">
        <Checkbox
          checked={blockUser}
          onCheckedChange={(checked) => setBlockUser(checked === true)}
        />
        {t('feed.safety.alsoBlock')}
      </label>

      <Button
        size="lg"
        block
        disabled={reason === null || pending}
        onClick={() => reason !== null && onSubmit(reason, comment, blockUser)}
      >
        {t('feed.safety.submit')}
      </Button>

      <p className="text-center text-tiny text-faint">{t('feed.safety.reviewNote')}</p>
    </div>
  )
}

type SectionProps = {
  title: string
  /** Данных нет — вместо содержимого объясняем это внутри самой секции. */
  empty: boolean
  children: ReactNode
}

function Section({ title, empty, children }: SectionProps) {
  const { t } = useTranslation()

  return (
    <section className="flex flex-col gap-1.5">
      <h3 className="text-tiny tracking-wide text-faint uppercase">{title}</h3>
      {empty ? (
        <p className="text-base text-faint">{t('feed.section.empty')}</p>
      ) : (
        <div className="text-base text-foreground">{children}</div>
      )}
    </section>
  )
}
