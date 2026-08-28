import { useNavigate, useParams } from '@tanstack/react-router'
import { ArrowLeft, Lock } from 'lucide-react'
import { useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  useAnswerQuestion,
  useQuestionOfDay,
  useQuestionsArchive,
  type QuestionArchiveItem,
} from '@/domains/matches'
import { ROUTES } from '@/shared/config'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card, EmptyState, ErrorState, Input, Skeleton } from '@/shared/ui'

/**
 * Вопрос дня (S-37). Ответы открываются только когда ответили оба — до этого
 * ответ собеседника не приходит с сервера вовсе, скрывать нечего.
 */
export function QuestionOfDayPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const { matchId } = useParams({ from: ROUTES.matchQuestion })

  const question = useQuestionOfDay(matchId)
  const archive = useQuestionsArchive(matchId)
  const answer = useAnswerQuestion()
  const [text, setText] = useState('')

  const goBack = useCallback(
    () => void navigate({ to: ROUTES.matchHub, params: { matchId } }),
    [navigate, matchId],
  )
  useBackButton(goBack)

  const handleSend = (): void => {
    const trimmed = text.trim()
    if (trimmed === '') return

    haptic.tap()
    answer.mutate({ matchId, text: trimmed }, { onSuccess: () => setText('') })
  }

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('matches.question.title')}</h1>
      </div>

      {question.isPending && <Skeleton className="h-40 w-full rounded-md" />}
      {question.isError && <ErrorState onRetry={() => void question.refetch()} />}

      {question.data && !question.data.available && (
        <EmptyState
          icon={Lock}
          title={t('matches.question.noneTitle')}
          description={t('matches.question.noneDescription')}
        />
      )}

      {question.data?.available && (
        <>
          <Card padding="tight" className="flex flex-col gap-1">
            <span className="text-eyebrow font-bold text-muted-foreground uppercase">
              {t('matches.question.today')}
            </span>
            <span className="text-base font-semibold text-balance">
              {question.data.questionText}
            </span>
          </Card>

          {question.data.myAnswer === null ? (
            <div className="flex flex-col gap-2">
              <Input
                value={text}
                onChange={(event) => setText(event.target.value)}
                placeholder={t('matches.question.placeholder')}
                className="h-11"
              />

              <Button
                size="lg"
                block
                disabled={text.trim() === '' || answer.isPending}
                onClick={handleSend}
              >
                {t('matches.question.send')}
              </Button>

              {answer.isError && (
                <p className="text-tiny text-destructive">{t('matches.question.sendError')}</p>
              )}
            </div>
          ) : (
            <Answers
              mine={question.data.myAnswer.text}
              theirs={question.data.partnerAnswer?.text ?? null}
            />
          )}
        </>
      )}

      {archive.data && archive.data.items.length > 0 && (
        <section className="flex flex-col gap-1.5">
          <h2 className="text-eyebrow font-bold text-muted-foreground uppercase">
            {t('matches.question.archive')}
          </h2>

          <div className="flex flex-col gap-2">
            {archive.data.items.map((item) => (
              <ArchiveCard key={item.questionId} item={item} />
            ))}
          </div>
        </section>
      )}
    </main>
  )
}

type AnswersProps = {
  mine: string
  /** `null` — собеседник ещё не ответил, его ответ сервер не отдаёт. */
  theirs: string | null
}

function Answers({ mine, theirs }: AnswersProps) {
  const { t } = useTranslation()

  return (
    <div className="flex flex-col gap-2">
      <Card padding="tight" className="flex flex-col gap-1">
        <span className="text-tiny text-faint">{t('matches.question.myAnswer')}</span>
        <span className="text-base">{mine}</span>
      </Card>

      <Card padding="tight" className="flex flex-col gap-1">
        <span className="text-tiny text-faint">{t('matches.question.theirAnswer')}</span>
        {theirs === null ? (
          <span className="text-base text-faint">{t('matches.question.waitingForThem')}</span>
        ) : (
          <span className="text-base">{theirs}</span>
        )}
      </Card>
    </div>
  )
}

type ArchiveCardProps = {
  item: QuestionArchiveItem
}

function ArchiveCard({ item }: ArchiveCardProps) {
  const { t } = useTranslation()

  return (
    <Card padding="tight" className="flex flex-col gap-1.5">
      <span className="text-base font-semibold text-balance">{item.questionText}</span>

      <span className="text-tiny text-muted-foreground">
        {item.myAnswer?.text ?? t('matches.question.noAnswer')}
      </span>

      {item.partnerAnswer && (
        <span className="text-tiny text-muted-foreground">{item.partnerAnswer.text}</span>
      )}
    </Card>
  )
}
