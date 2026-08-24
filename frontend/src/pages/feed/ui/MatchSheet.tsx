import { Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { type MatchPreview } from '@/domains/feed'
import { Button, ListRow } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'

type MatchSheetProps = {
  /** `null` — мэтча не было или экран закрыт. */
  match: MatchPreview | null
  onClose: () => void
}

/**
 * Взаимный лайк (S-16). Три лёгких входа в общение с оценкой усилий —
 * чтобы не оставлять человека перед пустым полем «напишите что-нибудь».
 *
 * Сами айсбрейкеры пока никуда не ведут: их ветки (вопрос дня, мини-игра,
 * идея свидания) — отдельные истории, см. docs/api-gaps.md.
 */
export function MatchSheet({ match, onClose }: MatchSheetProps) {
  const { t } = useTranslation()

  return (
    <Sheet open={match !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent side="bottom" closeLabel={t('action.close')} className="gap-0 rounded-t-xl p-0">
        {match && (
          <div className="flex flex-col gap-5 p-5 pb-safe">
            <div className="flex flex-col items-center gap-3 pt-2 text-center">
              <span
                className="flex size-16 items-center justify-center rounded-full bg-brand-soft"
                aria-hidden
              >
                <Sparkles className="size-8 text-brand" />
              </span>

              <SheetTitle className="text-display font-bold">{t('feed.match.title')}</SheetTitle>
              <SheetDescription className="text-base">{t('feed.match.subtitle')}</SheetDescription>
            </div>

            <div className="overflow-hidden rounded-lg border border-border">
              {match.icebreakers.map((icebreaker) => (
                <ListRow
                  key={icebreaker.type}
                  title={icebreaker.label}
                  subtitle={icebreaker.effort}
                />
              ))}
            </div>

            <Button variant="secondary" size="lg" block onClick={onClose}>
              {t('feed.match.later')}
            </Button>
          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}
