import { MessageCircleHeart } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { type LikeUser } from '@/domains/likes'
import { nameWithAge } from '@/shared/lib'

type SuperMessagesProps = {
  /** Только те, у кого есть суперсообщение — отбор делает вызывающий. */
  users: readonly LikeUser[]
  /** Открыть анкету, чтобы ответить лайком или отказом. */
  onOpen: (userId: string) => void
  /** Открыть хаб — для тех, с кем мэтч уже есть. */
  onOpenMatch: (matchId: string) => void
}

/**
 * Полученные суперсообщения — первым блоком во вкладке «Мэтчи».
 *
 * Живут здесь, а не в симпатиях: за суперсообщение уже заплатил отправитель,
 * и прятать его за платным раскрытием списка симпатий нечестно. Это
 * разблокированный лайк с текстом — по смыслу ближе к начатому разговору,
 * чем к строке платного списка.
 *
 * Текст показан целиком, а не бейджем на плитке: ради текста суперсообщение и
 * отправлялось, а в плитке 4:5 его было бы не прочитать. Не больше трёх строк —
 * длинное сообщение иначе выдавило бы из экрана сами мэтчи.
 */
export function SuperMessages({ users, onOpen, onOpenMatch }: SuperMessagesProps) {
  const { t } = useTranslation()

  if (users.length === 0) return null

  return (
    <section className="flex flex-col gap-2">
      <h2 className="text-eyebrow font-bold text-muted-foreground uppercase">
        {t('messages.super.sectionTitle')}
      </h2>

      <ul className="flex flex-col gap-2">
        {users.map((user) => (
          <li key={user.userId}>
            <button
              type="button"
              onClick={() =>
                user.isMatched && user.matchId !== null
                  ? onOpenMatch(user.matchId)
                  : onOpen(user.userId)
              }
              aria-label={
                user.isMatched
                  ? t('likes.openMatch', { name: user.name })
                  : t('feed.openProfile', { name: user.name })
              }
              className="flex w-full gap-3 rounded-lg bg-brand-soft p-3 text-left transition-colors duration-150 outline-none hover:bg-brand-soft/80 focus-visible:bg-brand-soft/80"
            >
              <span className="size-14 shrink-0 overflow-hidden rounded-md bg-gradient-photo-1">
                {user.mainPhotoUrl !== null && (
                  <img
                    src={user.mainPhotoUrl}
                    alt=""
                    loading="lazy"
                    className="size-full object-cover"
                  />
                )}
              </span>

              <span className="flex min-w-0 flex-1 flex-col gap-1">
                <span className="flex items-center gap-1.5 text-tiny font-semibold text-brand">
                  <MessageCircleHeart className="size-3.5 shrink-0" aria-hidden />
                  {t('messages.super.badge')}
                </span>

                <span className="truncate text-base font-bold text-foreground">
                  {nameWithAge(user.name, user.age)}
                </span>

                <span className="line-clamp-3 text-tiny text-foreground">
                  {user.superMessage?.text}
                </span>
              </span>
            </button>
          </li>
        ))}
      </ul>
    </section>
  )
}
