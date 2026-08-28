import { ArrowLeft } from 'lucide-react'
import { type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { Button } from '@/shared/ui'

import { StepDots } from './StepDots'

type OnboardingStepProps = {
  /** Номер шага, начиная с 1. Не передан — точки не показываются. */
  step?: number
  totalSteps?: number
  title: string
  description?: string
  children: ReactNode
  /** Подпись кнопки внизу экрана. */
  actionLabel: string
  onAction: () => void
  actionDisabled?: boolean
  /** Передан — в шапке появляется кнопка «Назад». */
  onBack?: () => void
  /** Сообщение об ошибке над кнопкой. */
  error?: string
}

/** Общий каркас шага анкеты: шапка с шагами, заголовок, контент и кнопка внизу. */
export function OnboardingStep({
  step,
  totalSteps = 5,
  title,
  description,
  children,
  actionLabel,
  onAction,
  actionDisabled,
  onBack,
  error,
}: OnboardingStepProps) {
  const { t } = useTranslation()

  return (
    <main className="flex flex-1 flex-col gap-3 px-5 pt-4">
      <div className="grid grid-cols-[2.75rem_1fr_2.75rem] items-center">
        {onBack ? (
          <Button
            variant="ghost"
            size="icon"
            onClick={onBack}
            aria-label={t('action.back')}
            className="border-0"
          >
            <ArrowLeft aria-hidden />
          </Button>
        ) : (
          <span />
        )}

        {step !== undefined && <StepDots current={step} total={totalSteps} />}
        <span />
      </div>

      <h1 className="text-heading text-display text-balance">{title}</h1>
      {description && <p className="text-base text-muted-foreground">{description}</p>}

      <div className="flex flex-col gap-4 pt-1">{children}</div>

      <div className="mt-auto flex flex-col gap-2 pt-4 pb-4">
        {error && <p className="text-center text-tiny text-destructive">{error}</p>}

        <Button size="lg" block disabled={actionDisabled} onClick={onAction}>
          {actionLabel}
        </Button>
      </div>
    </main>
  )
}
