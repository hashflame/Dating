import { useNavigate } from '@tanstack/react-router'
import { type ReactNode } from 'react'
import { Trans } from 'react-i18next'

import { ROUTES } from '@/shared/config'
import { Card, Checkbox } from '@/shared/ui'

type WelcomeConsentProps = {
  checked: boolean
  onCheckedChange: (checked: boolean) => void
}

/** Без согласия кнопка «Начать» неактивна — требование закона РБ №99-З. */
export function WelcomeConsent({ checked, onCheckedChange }: WelcomeConsentProps) {
  return (
    <Card padding="tight" className="flex flex-row items-center gap-3">
      <Checkbox
        id="consent"
        checked={checked}
        onCheckedChange={(value) => onCheckedChange(value === true)}
        className="mt-0.5 shrink-0"
      />

      <label htmlFor="consent" className="cursor-pointer text-tiny text-muted-foreground">
        <Trans
          i18nKey="welcome.consent.label"
          components={{
            terms: <LegalLink to={ROUTES.legalTerms} />,
            privacy: <LegalLink to={ROUTES.legalPrivacy} />,
          }}
        />
      </label>
    </Card>
  )
}

type LegalLinkProps = {
  to: typeof ROUTES.legalTerms | typeof ROUTES.legalPrivacy
  children?: ReactNode
}

function LegalLink({ to, children }: LegalLinkProps) {
  const navigate = useNavigate()

  return (
    <button
      type="button"
      className="text-brand underline-offset-2 hover:underline"
      onClick={(event) => {
        // Ссылка внутри label чекбокса — иначе клик его переключит.
        event.preventDefault()
        event.stopPropagation()
        void navigate({ to })
      }}
    >
      {children}
    </button>
  )
}
