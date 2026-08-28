import { Lightbulb, Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useIdeas, useVoteIdea, type IdeaTab } from '@/domains/ideas'
import { Button, EmptyState, ErrorState, Skeleton } from '@/shared/ui'
import { SegmentedControl } from '@/shared/ui/SegmentedControl'

import { IdeaCard } from './IdeaCard'
import { ProposeIdeaSheet } from './ProposeIdeaSheet'

/** Пустая доска говорит разное в зависимости от вкладки. */
const EMPTY_KEYS = {
  hot: 'ideas.empty.all',
  new: 'ideas.empty.all',
  inProgress: 'ideas.empty.inProgress',
  mine: 'ideas.empty.mine',
} as const

/**
 * Доска идей (S-60) — вкладка нижнего меню.
 *
 * Здесь предлагают, как улучшить приложение, и голосуют за чужие предложения.
 * Это не идеи свиданий: те живут в хабе мэтча.
 *
 * Экономический цикл виден на самом экране: за идею начисляют зорку, за
 * внедрённую — десять. Поэтому статус у каждой карточки на видном месте:
 * человек должен понимать, что предложение не улетело в пустоту.
 */
export function IdeasPage() {
  const { t } = useTranslation()

  const [tab, setTab] = useState<IdeaTab>('hot')
  const [proposing, setProposing] = useState(false)

  const ideas = useIdeas(tab)
  const vote = useVoteIdea()

  return (
    <main className="flex flex-col gap-3 px-4 pt-2 pb-6">
      <h1 className="text-heading text-display">{t('tabs.ideas')}</h1>

      <SegmentedControl
        value={tab}
        onValueChange={setTab}
        label={t('ideas.tabsLabel')}
        options={[
          { value: 'hot', label: t('ideas.tab.hot') },
          { value: 'new', label: t('ideas.tab.new') },
          { value: 'inProgress', label: t('ideas.tab.inProgress') },
          { value: 'mine', label: t('ideas.tab.mine') },
        ]}
      />

      {ideas.isPending && <Skeleton className="h-64 w-full rounded-md" />}
      {ideas.isError && <ErrorState onRetry={() => void ideas.refetch()} />}

      {ideas.data?.length === 0 && <EmptyState icon={Lightbulb} title={t(EMPTY_KEYS[tab])} />}

      {ideas.data?.map((idea) => (
        <IdeaCard
          key={idea.id}
          idea={idea}
          disabled={vote.isPending}
          onVote={() => vote.mutate({ ideaId: idea.id, voted: !idea.hasVoted })}
        />
      ))}

      {vote.isError && (
        <p className="text-center text-tiny text-destructive">{t('ideas.voteError')}</p>
      )}

      <Button size="lg" block onClick={() => setProposing(true)}>
        <Plus aria-hidden />
        {t('ideas.propose')}
      </Button>

      <ProposeIdeaSheet open={proposing} onClose={() => setProposing(false)} />
    </main>
  )
}
