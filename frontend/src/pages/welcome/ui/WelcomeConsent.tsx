import { type ReactNode } from 'react'
import { Trans } from 'react-i18next'

import { LEGAL_URLS } from '@/shared/config'
import { openExternalLink } from '@/shared/telegram'
import { Checkbox } from '@/shared/ui'

type WelcomeConsentProps = {
  checked: boolean
  onCheckedChange: (checked: boolean) => void
}

/** Без согласия кнопка «Начать» неактивна — требование закона РБ №99-З. */
export function WelcomeConsent({ checked, onCheckedChange }: WelcomeConsentProps) {
  return (
    <div className="flex items-start gap-3 rounded-lg border border-border bg-card p-3.5">
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
            terms: <LegalLink url={LEGAL_URLS.terms} />,
            privacy: <LegalLink url={LEGAL_URLS.privacy} />,
          }}
        />
      </label>
    </div>
  )
}

type LegalLinkProps = {
  url: string
  children?: ReactNode
}

function LegalLink({ url, children }: LegalLinkProps) {
  return (
    <button
      type="button"
      className="text-brand underline-offset-2 hover:underline"
      onClick={(event) => {
        // Ссылка внутри label чекбокса — иначе клик его переключит.
        event.preventDefault()
        event.stopPropagation()
        openExternalLink(url)
      }}
    >
      {children}
    </button>
  )
}
