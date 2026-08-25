import { BadgeCheck, Ban, Flag, Sparkles, Target, Users } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { REPORT_REASONS, useBlockUser, useReportUser } from '@/domains/moderation'
import { Button, Card, ListRow } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { Tag } from '@/shared/ui/Tag'

import { describeActivity } from '../lib/describe-activity'
import { distanceInKm } from '../lib/describe-place'
import { type FeedCard } from '../types/feed'

/** Типизированный `t()` не принимает шаблонную строку — держим ключи списком. */
const ACTIVITY_KEYS = {
  today: 'feed.lastActive.today',
  week: 'feed.lastActive.week',
  long: 'feed.lastActive.long',
} as const

type ProfileSheetProps = {
  /** `null` — шторка закрыта. Карточку держим снаружи, чтобы не терять анимацию закрытия. */
  card: FeedCard | null
  onClose: () => void
}

/**
 * Полная анкета (S-11). Открывается кнопкой с карточки ленты.
 *
 * Фото здесь нет намеренно: их только что смотрели на карточке, а шторка нужна
 * ради текста — совпадений, интересов, ценностей и ответов на вопросы.
 * Данные приходят вместе с лентой, отдельного запроса не нужно.
 */
export function ProfileSheet({ card, onClose }: ProfileSheetProps) {
  const { t } = useTranslation()

  return (
    <Sheet open={card !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex max-h-[92vh] flex-col gap-0 overflow-hidden rounded-t-xl border-0 p-0"
      >
        {card && <ProfileBody card={card} onClose={onClose} />}
      </SheetContent>
    </Sheet>
  )
}

type ProfileBodyProps = {
  card: FeedCard
  onClose: () => void
}

/**
 * Отдельный компонент, а не тело шторки: состояние жалобы должно сбрасываться
 * при смене карточки, а шторка остаётся смонтированной ради анимации закрытия.
 */
function ProfileBody({ card, onClose }: ProfileBodyProps) {
  const { t } = useTranslation()

  const km = distanceInKm(card)
  const place =
    km === null ? card.cityName : t('feed.cityWithDistance', { city: card.cityName, km })
  const activity = describeActivity(card.lastActive)

  return (
    // `min-h-0` — иначе в flex-колонке шторки тело не сжимается и обрезается
    // вместо прокрутки.
    <div className="flex min-h-0 flex-col gap-4 overflow-y-auto px-5 pt-5 pb-safe-5">
      <div className="flex flex-col gap-1">
        <SheetTitle className="flex items-center gap-2 text-display font-bold">
          {card.name}, {card.age}
          {card.isVerified && <BadgeCheck className="size-5 text-brand" aria-hidden />}
        </SheetTitle>

        <SheetDescription className="text-base">{place}</SheetDescription>
      </div>

      <Card padding="tight" className="flex flex-col gap-2">
        <span className="flex items-center gap-2 text-base font-semibold">
          <Sparkles className="size-4 text-brand" aria-hidden />
          {t('feed.compatibility', { score: card.compatibilityScore })}
        </span>

        <span className="flex flex-wrap gap-1.5">
          {card.compatibilitySummary.datingGoalMatch && (
            <Tag highlighted>
              <Target className="size-3" aria-hidden />
              {t('feed.summary.sameGoal')}
            </Tag>
          )}
          {card.compatibilitySummary.sharedInterestsCount > 0 && (
            <Tag highlighted>
              <Users className="size-3" aria-hidden />
              {t('feed.summary.sharedInterests', {
                count: card.compatibilitySummary.sharedInterestsCount,
              })}
            </Tag>
          )}
          {card.compatibilitySummary.bothVerified && (
            <Tag highlighted>
              <BadgeCheck className="size-3" aria-hidden />
              {t('feed.summary.bothVerified')}
            </Tag>
          )}
        </span>
      </Card>

      {/* Секции показываем всегда: пустая говорит о человеке не меньше,
          чем заполненная, а исчезающие блоки выглядят как недогруз. */}
      <Section title={t('feed.section.about')} empty={card.bio === null}>
        {card.bio}
      </Section>

      <Section title={t('feed.section.interests')} empty={card.interests.length === 0}>
        <span className="flex flex-wrap gap-1.5">
          {card.interests.map((interest) => (
            <Tag key={interest.id} highlighted={interest.isMatch}>
              {interest.name}
            </Tag>
          ))}
        </span>
      </Section>

      {/* Ценности и предпочтения на свидания есть в спеке (S-11), но лента их
          пока не отдаёт — см. docs/api-gaps.md. */}
      <Section title={t('feed.section.values')} empty>
        {null}
      </Section>

      <Section title={t('feed.section.datePrefs')} empty>
        {null}
      </Section>

      <Section title={t('feed.section.prompts')} empty={card.prompts.length === 0}>
        <span className="flex flex-col gap-3">
          {card.prompts.map((prompt, index) => (
            <span key={index} className="block">
              {prompt}
            </span>
          ))}
        </span>
      </Section>

      <Section title={t('feed.section.activity')} empty={activity === null}>
        {activity !== null && t(ACTIVITY_KEYS[activity])}
      </Section>

      <SafetyActions card={card} onBlocked={onClose} />
    </div>
  )
}

type SafetyActionsProps = {
  card: FeedCard
  onBlocked: () => void
}

/** Блокировка и жалоба (S-11, S-13). Внизу анкеты — их ищут, когда уже решили. */
function SafetyActions({ card, onBlocked }: SafetyActionsProps) {
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
        <div className="overflow-hidden rounded-xl border border-border">
          {REPORT_REASONS.map((reason) => (
            <ListRow
              key={reason.value}
              title={t(reason.labelKey)}
              onClick={() => report.mutate({ userId: card.userId, reason: reason.value })}
            />
          ))}
        </div>
      ) : (
        <div className="flex gap-2">
          <Button
            variant="secondary"
            size="sm"
            className="flex-1"
            disabled={block.isPending}
            onClick={() => block.mutate(card.userId, { onSuccess: onBlocked })}
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
