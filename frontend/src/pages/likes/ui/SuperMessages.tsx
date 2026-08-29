import { MessageCircleHeart } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { type LikeUser } from '@/domains/likes'
import { nameWithAge } from '@/shared/lib'

type SuperMessagesProps = {
  /** Только те, у кого есть суперсообщение — отбор делает вызывающий. */
  users: readonly LikeUser[]
  onOpen: (userId: string) => void
}

/**
 * Суперсообщения во входящих симпатиях (тикет «обновить логику сообщений»).
 *
 * Не бейдж на плитке, а отдельный блок над сеткой: суперсообщение — это текст,
 * и ради текста оно и отправлялось. В плитке 4:5 его было бы не прочитать, а
 * человек, потративший на сообщение зорки, вправе рассчитывать, что его
 * прочтут, а не увидят иконку.
 */
export function SuperMessages({ users, onOpen }: SuperMessagesProps) {
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
              onClick={() => onOpen(user.userId)}
              aria-label={t('feed.openProfile', { name: user.name })}
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

                {/* Текст целиком, но не больше трёх строк: длинное сообщение
                    иначе выдавило бы из списка всех остальных. */}
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
