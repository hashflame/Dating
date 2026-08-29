import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useCreateIdea } from '@/domains/ideas'
import { useHaptic } from '@/shared/telegram'
import { AutoTextarea, Button, Checkbox } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'

/** Столько же, сколько у комментария к жалобе: длиннее никто не читает. */
const MAX_LENGTH = 1000
const MIN_LENGTH = 10

type ProposeIdeaSheetProps = {
  open: boolean
  onClose: () => void
}

/**
 * «Предложить идею» (S-60).
 *
 * Анонимность — про имя рядом с идеей, а не про автора вообще: зорки всё равно
 * начисляются, поэтому в подписи это сказано прямо.
 */
export function ProposeIdeaSheet({ open, onClose }: ProposeIdeaSheetProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()

  const create = useCreateIdea()
  const [text, setText] = useState('')
  const [anonymous, setAnonymous] = useState(false)

  const close = (): void => {
    setText('')
    setAnonymous(false)
    create.reset()
    onClose()
  }

  const handleSubmit = (): void => {
    haptic.tap()
    create.mutate(
      { text: text.trim(), anonymous },
      { onSuccess: () => haptic.success(), onError: () => haptic.error() },
    )
  }

  return (
    <Sheet open={open} onOpenChange={(next) => !next && close()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex flex-col gap-0 rounded-t-xl border-0 p-0"
      >
        <div className="flex flex-col gap-4 px-5 pt-5 pb-safe-5">
          <div className="flex flex-col gap-1">
            <SheetTitle className="text-display font-bold">{t('ideas.proposeTitle')}</SheetTitle>
            <SheetDescription className="text-base">{t('ideas.proposeHint')}</SheetDescription>
          </div>

          {create.isSuccess ? (
            <>
              <p className="text-base text-foreground">
                {create.data.sparksAwarded > 0
                  ? t('ideas.submittedWithReward', { amount: create.data.sparksAwarded })
                  : t('ideas.submittedNoReward')}
              </p>

              <Button size="lg" block onClick={close}>
                {t('action.done')}
              </Button>
            </>
          ) : (
            <>
              <AutoTextarea
                value={text}
                onChange={(event) => setText(event.target.value)}
                placeholder={t('ideas.proposePlaceholder')}
                maxLength={MAX_LENGTH}
                rows={5}
                autoFocus
              />

              <label className="flex items-center gap-2 text-base">
                <Checkbox
                  checked={anonymous}
                  onCheckedChange={(checked) => setAnonymous(checked === true)}
                />
                {t('ideas.anonymousToggle')}
              </label>

              <Button
                size="lg"
                block
                disabled={text.trim().length < MIN_LENGTH || create.isPending}
                onClick={handleSubmit}
              >
                {t('ideas.submit')}
              </Button>

              {create.isError && (
                <p className="text-center text-tiny text-destructive">{t('ideas.submitError')}</p>
              )}
            </>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
