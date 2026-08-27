import { useQuery, type UseQueryResult } from '@tanstack/react-query'

import { stub } from '@/shared/api'

import { IN_PROGRESS_STATUSES, type Idea, type IdeaTab } from '../types/idea'

import { ideaKeys } from './idea-keys'

/**
 * Фикстура доски: разные статусы, длинный и короткий текст, анонимный автор,
 * своя идея, идея без единого голоса. Уедет вместе с заглушкой.
 */
const FIXTURE: Idea[] = [
  {
    id: '5e6b1a2c-0001-4000-8000-000000000001',
    text: 'Добавить кнопку «вернуть последний свайп» — постоянно промахиваюсь пальцем',
    status: 'implemented',
    votesCount: 214,
    hasVoted: true,
    authorName: 'Дмитрий',
    isMine: false,
    createdAt: '2026-06-14T09:12:00+00:00',
  },
  {
    id: '5e6b1a2c-0001-4000-8000-000000000002',
    text: 'Голосовое приветствие в анкете — по голосу сразу понятно, свой человек или нет',
    status: 'planned',
    votesCount: 158,
    hasVoted: false,
    authorName: null,
    isMine: false,
    createdAt: '2026-07-02T18:40:00+00:00',
  },
  {
    id: '5e6b1a2c-0001-4000-8000-000000000003',
    text: 'Совместный плейлист для мэтча — сразу видно вкусы и есть о чём поговорить',
    status: 'underReview',
    votesCount: 96,
    hasVoted: false,
    authorName: 'Марина',
    isMine: false,
    createdAt: '2026-08-11T11:05:00+00:00',
  },
  {
    id: '5e6b1a2c-0001-4000-8000-000000000004',
    text: 'Хочу видеть, в каком районе человек живёт, а не только город — Минск большой, и ехать через весь город на первое свидание никто не хочет. Достаточно района, точный адрес не нужен',
    status: 'new',
    votesCount: 12,
    hasVoted: false,
    authorName: null,
    isMine: true,
    createdAt: '2026-08-24T20:15:00+00:00',
  },
  {
    id: '5e6b1a2c-0001-4000-8000-000000000005',
    text: 'Сделать тёмную тему поярче',
    status: 'declined',
    votesCount: 0,
    hasVoted: false,
    authorName: 'Игорь',
    isMine: false,
    createdAt: '2026-08-25T08:00:00+00:00',
  },
]

/** Порядок вкладки: «популярные» — по голосам, остальные — новые сверху. */
function selectTab(ideas: Idea[], tab: IdeaTab): Idea[] {
  const byNewest = [...ideas].sort((a, b) => b.createdAt.localeCompare(a.createdAt))

  if (tab === 'hot') return [...ideas].sort((a, b) => b.votesCount - a.votesCount)
  if (tab === 'new') return byNewest
  if (tab === 'mine') return byNewest.filter((idea) => idea.isMine)

  return byNewest.filter((idea) => IN_PROGRESS_STATUSES.includes(idea.status))
}

/**
 * Доска идей (S-60).
 *
 * Отбор по вкладке делает заглушка, но так же его должен делать и сервер:
 * `?sort=hot|new` из T-19.1 не покрывает «В работе» и «Мои» — нужны фильтры
 * по статусу и по автору, см. docs/api-gaps.md.
 */
export function useIdeas(tab: IdeaTab): UseQueryResult<Idea[], Error> {
  return useQuery({
    queryKey: ideaKeys.list(tab),
    // @stub: GET /api/ideas на бэкенде нет (T-19.1) — см. docs/api-gaps.md
    queryFn: () => stub(`GET /api/ideas?tab=${tab}`, selectTab(FIXTURE, tab)),
  })
}
