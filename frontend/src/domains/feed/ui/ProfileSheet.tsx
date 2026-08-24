import { BadgeCheck, Sparkles, Target, Users } from 'lucide-react'
import { type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { Card } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { PhotoCarousel } from '@/shared/ui/PhotoCarousel'
import { Tag } from '@/shared/ui/Tag'

import { distanceInKm } from '../lib/describe-place'
import { type FeedCard } from '../types/feed'

type ProfileSheetProps = {
  /** `null` — шторка закрыта. Карточку держим снаружи, чтобы не терять анимацию закрытия. */
  card: FeedCard | null
  onClose: () => void
}

/**
 * Полная анкета (S-11). Открывается тапом по карточке ленты.
 * Данные приходят вместе с лентой — отдельного запроса не нужно.
 */
export function ProfileSheet({ card, onClose }: ProfileSheetProps) {
  const { t } = useTranslation()

  const describePlace = (item: FeedCard): string => {
    const km = distanceInKm(item)

    return km === null ? item.cityName : t('feed.cityWithDistance', { city: item.cityName, km })
  }

  return (
    <Sheet open={card !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="max-h-[92vh] gap-0 overflow-y-auto rounded-t-xl border-0 p-0"
      >
        {card && (
          <>
            <PhotoCarousel
              urls={card.photos.map((photo) => photo.mediumUrl)}
              label={card.name}
              className="aspect-[4/5] w-full shrink-0"
            />

            <div className="flex flex-col gap-4 p-5">
              <div className="flex flex-col gap-1">
                <SheetTitle className="flex items-center gap-2 text-display font-bold">
                  {card.name}, {card.age}
                  {card.isVerified && <BadgeCheck className="size-5 text-brand" aria-hidden />}
                </SheetTitle>

                <SheetDescription className="text-base">{describePlace(card)}</SheetDescription>
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

              {card.bio && <Section title={t('feed.section.about')}>{card.bio}</Section>}

              {card.interests.length > 0 && (
                <Section title={t('feed.section.interests')}>
                  <span className="flex flex-wrap gap-1.5">
                    {card.interests.map((interest) => (
                      <Tag key={interest.id} highlighted={interest.isMatch}>
                        {interest.name}
                      </Tag>
                    ))}
                  </span>
                </Section>
              )}

              {card.prompts.map((prompt, index) => (
                <Section key={index} title={t('feed.section.prompt')}>
                  {prompt}
                </Section>
              ))}
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  )
}

type SectionProps = {
  title: string
  children: ReactNode
}

function Section({ title, children }: SectionProps) {
  return (
    <section className="flex flex-col gap-1.5">
      <h3 className="text-tiny tracking-wide text-faint uppercase">{title}</h3>
      <div className="text-base text-foreground">{children}</div>
    </section>
  )
}
