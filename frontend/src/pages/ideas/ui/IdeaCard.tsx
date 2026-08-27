import { ThumbsUp } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { type Idea } from '@/domains/ideas'
import { cn } from '@/shared/lib'
import { Card } from '@/shared/ui'
import { Tag } from '@/shared/ui/Tag'

/** Типизированный `t()` не принимает шаблонную строку — держим ключи списком. */
const STATUS_KEYS = {
  new: 'ideas.status.new',
  underReview: 'ideas.status.underReview',
  planned: 'ideas.status.planned',
  implemented: 'ideas.status.implemented',
  declined: 'ideas.status.declined',
} as const

type IdeaCardProps = {
  idea: Idea
  onVote: () => void
  disabled: boolean
}

/**
 * Идея на доске (S-60): статус, счётчик голосов, текст и автор.
 *
 * Голос — кнопка со счётчиком, а не отдельная иконка рядом с числом: нажимают
 * именно «плюс один голос», и промахиваться по мелкой иконке не должно.
 */
export function IdeaCard({ idea, onVote, disabled }: IdeaCardProps) {
  const { t } = useTranslation()

  return (
    <Card padding="tight" className="flex flex-col gap-2">
      <span className="flex items-center justify-between gap-2">
        <Tag highlighted={idea.status === 'implemented'}>{t(STATUS_KEYS[idea.status])}</Tag>

        <button
          type="button"
          onClick={onVote}
          disabled={disabled}
          aria-pressed={idea.hasVoted}
          aria-label={t('ideas.vote')}
          className={cn(
            'flex h-8 shrink-0 items-center gap-1.5 rounded-full px-3 text-sm font-semibold',
            'transition-colors duration-150 disabled:opacity-60',
            idea.hasVoted ? 'bg-brand-soft text-brand' : 'bg-tag text-muted-foreground',
          )}
        >
          <ThumbsUp className="size-4" aria-hidden />
          {idea.votesCount}
        </button>
      </span>

      <span className="text-base text-foreground">{idea.text}</span>

      <span className="text-tiny text-faint">
        {idea.authorName ?? t('ideas.anonymous')}
        {idea.status === 'implemented' && ` · ${t('ideas.authorReward')}`}
      </span>
    </Card>
  )
}
