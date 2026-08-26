import { useNavigate } from '@tanstack/react-router'
import { Heart } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { type MatchPreview } from '@/domains/feed'
import { useUnlockContact } from '@/domains/matches'
import { isApiError } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { cn } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'
import { Button } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { ComposeSheet } from '@/widgets/compose-sheet'

type MatchSheetProps = {
  /** `null` — мэтча не было или экран закрыт. */
  match: MatchPreview | null
  /** Фото пары: слева наше, справа собеседника. `null` — вместо фото градиент. */
  ownPhotoUrl: string | null
  partnerPhotoUrl: string | null
  onClose: () => void
}

/**
 * Взаимный лайк (S-16).
 *
 * Главное действие — «Написать»: оно открывает контакт за зорки, затем ведёт
 * в шторку составления первого сообщения (S-33). Айсбрейкеры рядом — не
 * соперник ему, а подсказка для тех, кто не знает, с чего начать, поэтому они
 * мелкие, приглушённые и без своей заливки.
 */
export function MatchSheet({ match, ownPhotoUrl, partnerPhotoUrl, onClose }: MatchSheetProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const unlock = useUnlockContact()
  const [error, setError] = useState<string | null>(null)
  /** `undefined` — контакт ещё не открыт, шторка сообщения не показана. */
  const [composeLink, setComposeLink] = useState<string | null | undefined>(undefined)

  /** Ветки айсбрейкеров живут в хабе мэтча (S-31) — туда и ведём. */
  const openHub = (matchId: string): void => {
    haptic.tap()
    void navigate({ to: ROUTES.matchHub, params: { matchId } })
  }

  /**
   * После открытия контакта ведём не сразу в Telegram, а в шторку составления
   * первого сообщения (S-33) — там же заготовки из анкеты.
   */
  const handleWrite = (): void => {
    if (!match) return

    haptic.tap()
    setError(null)
    unlock.mutate(match.matchId, {
      onSuccess: (contact) => {
        haptic.success()
        const url =
          contact.deepLink ??
          (contact.telegramUsername === null ? null : `https://t.me/${contact.telegramUsername}`)

        setComposeLink(url)
      },
      onError: (reason) => {
        haptic.error()
        setError(
          isApiError(reason) && reason.status === 402
            ? t('feed.match.noSparks')
            : t('feed.match.unlockError'),
        )
      },
    })
  }

  const closeCompose = (): void => {
    setComposeLink(undefined)
    onClose()
  }

  return (
    <>
      <Sheet
        open={match !== null && composeLink === undefined}
        onOpenChange={(open) => !open && onClose()}
      >
        <SheetContent
          side="bottom"
          closeLabel={t('action.close')}
          className="flex max-h-[92vh] flex-col gap-0 overflow-hidden rounded-t-xl p-0"
        >
          {match && (
            <div className="flex min-h-0 flex-col gap-5 overflow-y-auto px-5 pt-5 pb-safe-5">
              <div className="flex flex-col items-center gap-3 pt-2 text-center">
                <span className="rounded-full bg-brand-soft px-3 py-1 text-tiny font-semibold text-brand">
                  {t('feed.match.pill')}
                </span>

                <span className="flex items-center" aria-hidden>
                  <Avatar url={ownPhotoUrl} className="-mr-3" />
                  <Heart className="z-10 size-6 fill-brand text-brand" />
                  <Avatar url={partnerPhotoUrl} className="-ml-3" />
                </span>

                <SheetTitle className="text-display font-bold text-balance">
                  {t('feed.match.title')}
                </SheetTitle>

                <SheetDescription className="text-base">
                  {t('feed.match.subtitle')}
                </SheetDescription>
              </div>

              {match.icebreakers.length > 0 && (
                <section className="flex flex-col gap-2">
                  <h3 className="text-tiny tracking-wide text-faint uppercase">
                    {t('feed.match.icebreakersTitle')}
                  </h3>

                  <div className="grid grid-cols-2 gap-2">
                    {match.icebreakers.map((icebreaker) => (
                      <button
                        key={icebreaker.type}
                        type="button"
                        onClick={() => openHub(match.matchId)}
                        className="flex min-h-11 flex-col items-start justify-center gap-0.5 rounded-md border border-border px-3 py-2 text-left transition-colors hover:bg-accent"
                      >
                        <span className="text-tiny font-semibold text-foreground">
                          {icebreaker.label}
                        </span>
                        <span className="text-micro text-faint">{icebreaker.effort}</span>
                      </button>
                    ))}
                  </div>
                </section>
              )}

              {error !== null && <p className="text-center text-tiny text-destructive">{error}</p>}

              <div className="flex flex-col gap-2">
                <Button size="lg" block onClick={handleWrite} disabled={unlock.isPending}>
                  {t('feed.match.write')}
                </Button>

                <Button variant="ghost" size="lg" block onClick={onClose}>
                  {t('feed.match.later')}
                </Button>
              </div>
            </div>
          )}
        </SheetContent>
      </Sheet>

      {match && (
        <ComposeSheet
          open={composeLink !== undefined}
          onClose={closeCompose}
          userId={match.userId}
          name={match.name}
          link={composeLink ?? null}
        />
      )}
    </>
  )
}

type AvatarProps = {
  url: string | null
  className: string
}

function Avatar({ url, className }: AvatarProps) {
  return (
    <span
      className={cn(
        'size-20 overflow-hidden rounded-xl border-2 border-brand bg-gradient-photo-1',
        className,
      )}
    >
      {url !== null && <img src={url} alt="" className="size-full object-cover" />}
    </span>
  )
}
