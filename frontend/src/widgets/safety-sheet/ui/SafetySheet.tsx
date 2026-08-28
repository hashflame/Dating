import { Ban, Flag } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  REPORT_REASONS,
  useBlockUser,
  useReportUser,
  type ReportReason,
} from '@/domains/moderation'
import { Button, Checkbox, ListRow } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { Textarea } from '@/shared/ui/kit/textarea'

type SafetySheetProps = {
  /** `null` — шторка закрыта. Держим id снаружи, чтобы не терять анимацию. */
  userId: string | null
  name: string
  onClose: () => void
  /** Человека заблокировали — вызывающему обычно надо закрыть и анкету. */
  onBlocked?: () => void
}

/**
 * Блокировка и жалоба (S-11, S-13) отдельной шторкой.
 *
 * Раньше это был блок в самом низу анкеты: чтобы пожаловаться, приходилось
 * листать «О себе», интересы и ответы человека, на которого жалуешься. Здесь
 * только то, что относится к решению, и ничего больше.
 *
 * Один виджет на все места, где такое действие уместно: лента, симпатии, хаб
 * мэтча. Раньше в хабе его не было вовсе — пожаловаться после мэтча было негде.
 */
export function SafetySheet({ userId, name, onClose, onBlocked }: SafetySheetProps) {
  const { t } = useTranslation()

  return (
    <Sheet open={userId !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex max-h-[92vh] flex-col gap-0 overflow-hidden rounded-t-xl border-0 p-0"
      >
        {userId !== null && (
          <SafetyBody userId={userId} name={name} onClose={onClose} onBlocked={onBlocked} />
        )}
      </SheetContent>
    </Sheet>
  )
}

type SafetyBodyProps = {
  userId: string
  name: string
  onClose: () => void
  onBlocked?: () => void
}

/**
 * Отдельный компонент, а не тело шторки: выбранная причина и комментарий должны
 * сбрасываться при смене человека, а шторка остаётся смонтированной ради
 * анимации закрытия.
 */
function SafetyBody({ userId, name, onClose, onBlocked }: SafetyBodyProps) {
  const { t } = useTranslation()

  const block = useBlockUser()
  const report = useReportUser()
  const [reporting, setReporting] = useState(false)

  const done = block.isSuccess || report.isSuccess

  return (
    <div className="flex min-h-0 flex-col gap-4 overflow-y-auto px-5 pt-5 pb-safe-5">
      <div className="flex flex-col gap-1">
        <SheetTitle className="text-display font-bold">{t('feed.safety.title')}</SheetTitle>
        <SheetDescription className="text-base">
          {t('feed.safety.about', { name })}
        </SheetDescription>
      </div>

      {done && (
        <>
          <p className="text-base text-foreground">
            {block.isSuccess ? t('feed.safety.blocked') : t('feed.safety.reported')}
          </p>

          <Button size="lg" block onClick={onClose}>
            {t('action.done')}
          </Button>
        </>
      )}

      {!done && !reporting && (
        <>
          <Button
            variant="secondary"
            size="lg"
            block
            disabled={block.isPending}
            onClick={() =>
              block.mutate(userId, {
                onSuccess: () => onBlocked?.(),
              })
            }
          >
            <Ban aria-hidden />
            {t('feed.safety.block')}
          </Button>

          <Button variant="secondary" size="lg" block onClick={() => setReporting(true)}>
            <Flag aria-hidden />
            {t('feed.safety.report')}
          </Button>
        </>
      )}

      {!done && reporting && (
        <ReportForm
          pending={report.isPending}
          onSubmit={(reason, comment, blockUser) =>
            report.mutate({ userId, reason, comment, blockUser })
          }
        />
      )}

      {(block.isError || report.isError) && (
        <p className="text-tiny text-destructive">{t('feed.safety.error')}</p>
      )}
    </div>
  )
}

type ReportFormProps = {
  pending: boolean
  onSubmit: (reason: ReportReason, comment: string, blockUser: boolean) => void
}

/**
 * Форма жалобы (S-13): причина, необязательный комментарий и отдельное решение
 * «заблокировать заодно» — сервер принимает его тем же запросом (T-17.1).
 *
 * Причина одна: критичные (`underage`, `unsafeMeeting`) уводят аккаунт в
 * блокировку немедленно, поэтому смешивать их с остальными в одном отчёте
 * нельзя — модератор должен видеть, на что именно жалуются.
 */
function ReportForm({ pending, onSubmit }: ReportFormProps) {
  const { t } = useTranslation()
  const [reason, setReason] = useState<ReportReason | null>(null)
  const [comment, setComment] = useState('')
  const [blockUser, setBlockUser] = useState(true)

  return (
    <div className="flex flex-col gap-3">
      <div className="overflow-hidden rounded-md bg-surface">
        {REPORT_REASONS.map((item) => (
          <ListRow
            key={item.value}
            title={t(item.labelKey)}
            selected={reason === item.value}
            onClick={() => setReason(item.value)}
          />
        ))}
      </div>

      <Textarea
        value={comment}
        onChange={(event) => setComment(event.target.value)}
        placeholder={t('feed.safety.commentPlaceholder')}
        maxLength={1000}
        rows={3}
      />

      <label className="flex items-center gap-2 text-base">
        <Checkbox
          checked={blockUser}
          onCheckedChange={(checked) => setBlockUser(checked === true)}
        />
        {t('feed.safety.alsoBlock')}
      </label>

      <Button
        size="lg"
        block
        disabled={reason === null || pending}
        onClick={() => reason !== null && onSubmit(reason, comment, blockUser)}
      >
        {t('feed.safety.submit')}
      </Button>

      <p className="text-center text-tiny text-faint">{t('feed.safety.reviewNote')}</p>
    </div>
  )
}
