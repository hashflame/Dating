import { BadgeCheck, Heart, ShieldAlert, Sparkles, Target, Users, X } from 'lucide-react'
import { type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { cn, distanceInKm, hasAnsweredPrompt, nameWithAge, pickQuickQuestions } from '@/shared/lib'
import { Button, Card } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { Tag } from '@/shared/ui/Tag'

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
}

type ProfileSheetProps = {
  /** `null` — шторка закрыта. Анкету держим снаружи, чтобы не терять анимацию закрытия. */
  profile: ProfileDetails | null
  onClose: () => void
  /** Своя анкета в режиме «как видят другие»: блокировка и жалоба тут не к месту. */
  own?: boolean
  /**
   * Открыть шторку безопасности. Сама анкета её больше не показывает: чтобы
   * пожаловаться, приходилось листать интересы и ответы того, на кого жалуешься.
   */
  onSafety?: () => void
  /**
   * Ответить на симпатию прямо из анкеты. Передаются вместе: выбор без второй
   * половины — это не выбор. Во входящих симпатиях анкета — единственное
   * место, где можно ответить: в ленте эти люди уже не появятся.
   */
  decision?: {
    onLike: () => void
    onDislike: () => void
    pending: boolean
    /** Сервер отказал — текст показываем прямо над кнопками. */
    error?: string
  }
}

/**
 * Полная анкета (S-11). Открывается кнопкой с карточки ленты и тапом по
 * человеку в симпатиях.
 *
 * Фото здесь нет намеренно: их только что смотрели на карточке, а шторка нужна
 * ради текста — совпадений, интересов, ценностей и ответов на вопросы.
 */
export function ProfileSheet({
  profile,
  onClose,
  own = false,
  onSafety,
  decision,
}: ProfileSheetProps) {
  const { t } = useTranslation()

  return (
    <Sheet open={profile !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex max-h-[92vh] flex-col gap-0 overflow-hidden rounded-t-xl border-0 p-0"
      >
        {profile && (
          <ProfileBody profile={profile} own={own} onSafety={onSafety} decision={decision} />
        )}
      </SheetContent>
    </Sheet>
  )
}

type ProfileBodyProps = {
  profile: ProfileDetails
  own: boolean
  onSafety?: () => void
  decision?: ProfileSheetProps['decision']
}

/**
 * Отдельный компонент, а не тело шторки: состояние жалобы должно сбрасываться
 * при смене анкеты, а шторка остаётся смонтированной ради анимации закрытия.
 */
function ProfileBody({ profile, own, onSafety, decision }: ProfileBodyProps) {
  const { t } = useTranslation()

  const km = distanceInKm(profile.distanceKm)
  const place =
    km === null ? profile.cityName : t('feed.cityWithDistance', { city: profile.cityName, km })
  const summary = profile.compatibilitySummary
  const quickQuestions = pickQuickQuestions(profile.userId)

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

      <Section title={t('feed.section.interests')} empty={profile.interests.length === 0} bare>
        <div className="grid grid-cols-3 gap-2">
          {profile.interests.map((interest) => (
            <span
              key={interest.id}
              className={cn(
                'flex min-h-9 items-center justify-center rounded-md px-2 py-1.5 text-center text-tiny font-semibold break-words',
                interest.isMatch === true
                  ? 'bg-brand-soft text-brand'
                  : 'bg-surface text-foreground',
              )}
            >
              {interest.name}
            </span>
          ))}
        </div>
      </Section>

      {/* Ценности и предпочтения на свидания есть в спеке (S-11), но ни лента,
          ни анкета их пока не отдают — см. docs/api-gaps.md. */}
      <Section title={t('feed.section.values')} empty>
        {null}
      </Section>

      <Section title={t('feed.section.datePrefs')} empty>
        {null}
      </Section>

      <Section title={t('feed.section.prompts')} empty={!hasAnsweredPrompt(profile.prompts)} bare>
        <div className="flex flex-col gap-2">
          {profile.prompts.map(
            (prompt, index) =>
              prompt.trim() !== '' && (
                <div key={index} className="flex flex-col gap-1 rounded-md bg-surface p-3">
                  {quickQuestions[index] && (
                    <span className="text-tiny font-semibold text-muted-foreground">
                      {t(quickQuestions[index].labelKey)}
                    </span>
                  )}
                  <span className="text-base">{prompt}</span>
                </div>
              ),
          )}
        </div>
      </Section>

      {!own && onSafety && (
        <Button variant="secondary" size="sm" block onClick={onSafety}>
          <ShieldAlert aria-hidden />
          {t('feed.safety.open')}
        </Button>
      )}

      {decision && (
        /* Липнет к низу шторки: отвечают, не дочитав анкету до конца, — и это
           то, ради чего её открыли из симпатий. */
        <div className="sticky bottom-0 -mx-5 flex flex-col gap-1 bg-background px-5 pt-3">
          <p className="min-h-4 text-center text-tiny text-destructive" aria-live="polite">
            {decision.error}
          </p>

          {/* Сетка, а не flex-ряд: у обеих кнопок `block` (`w-full`), а базовый
              класс кнопки — `shrink-0`, поэтому в ряду они занимали по 100%
              ширины каждая и распирали шторку горизонтальной прокруткой.
              Колонки `minmax(0, 1fr)` делят ряд без переполнения, а пропорция
              1:3 отдаёт вес «Нравится»: отказ здесь не равноценное действие. */}
          <div className="grid grid-cols-[1fr_3fr] gap-3">
            {/* Только иконка: подпись съедала ширину, а крестик рядом с
                «Нравится» читается однозначно. Название уезжает в `aria-label`,
                иначе кнопка немая для скринридера. */}
            <Button
              variant="secondary"
              size="lg"
              block
              aria-label={t('feed.action.dislike')}
              disabled={decision.pending}
              onClick={decision.onDislike}
            >
              <X aria-hidden />
            </Button>

            <Button size="lg" block disabled={decision.pending} onClick={decision.onLike}>
              <Heart aria-hidden />
              {t('feed.action.like')}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

type SectionProps = {
  title: string
  /** Данных нет — вместо содержимого объясняем это внутри самой секции. */
  empty: boolean
  children: ReactNode
  /**
   * Без обёрточной карточки: содержимое само разбито на подсвеченные блоки
   * (см. «Из анкеты»), и карточка вокруг них была бы рамкой вокруг рамок.
   */
  bare?: boolean
}

function Section({ title, empty, children, bare = false }: SectionProps) {
  const { t } = useTranslation()

  const body = empty ? (
    <p className="text-base text-faint">{t('feed.section.empty')}</p>
  ) : (
    <div className="text-base text-foreground">{children}</div>
  )

  return (
    <section className="flex flex-col gap-2">
      <h3 className="px-1 text-eyebrow font-bold text-muted-foreground uppercase">{title}</h3>

      {bare ? body : <Card padding="tight">{body}</Card>}
    </section>
  )
}
