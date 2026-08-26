import { Check, Copy } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useUserProfile } from '@/domains/profiles'
import { openExternalLink, useHaptic } from '@/shared/telegram'
import { Button, Skeleton } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { Textarea } from '@/shared/ui/kit/textarea'

import { buildMessageTemplates, type MessageTemplate } from '../lib/message-templates'

type ComposeSheetProps = {
  open: boolean
  onClose: () => void
  /** Кому пишем — за его анкетой идём, чтобы собрать заготовки. */
  userId: string
  name: string
  /** Куда уводить: ссылка появляется только после открытия контакта. */
  link: string | null
}

/**
 * Сообщение мэтчу (S-33): заготовки из анкеты, правка текста, копирование и
 * переход в Telegram.
 *
 * Бэкенд сообщение не отправляет и по спеке не должен: текст копируется в буфер,
 * дальше пользователь пишет сам в Telegram. AI-вариантов пока нет
 * (`POST /api/ai/generate-message` не реализован), поэтому заготовки собираются
 * на клиенте из реальных данных анкеты — см. `lib/message-templates.ts`.
 */
export function ComposeSheet({ open, onClose, userId, name, link }: ComposeSheetProps) {
  const { t } = useTranslation()

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex max-h-[92vh] flex-col gap-0 overflow-hidden rounded-t-xl border-0 p-0"
      >
        {open && <ComposeBody userId={userId} name={name} link={link} />}
      </SheetContent>
    </Sheet>
  )
}

type ComposeBodyProps = {
  userId: string
  name: string
  link: string | null
}

/**
 * Отдельный компонент: черновик должен начинаться заново при каждом открытии,
 * а сама шторка остаётся смонтированной ради анимации закрытия.
 */
function ComposeBody({ userId, name, link }: ComposeBodyProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()

  const profile = useUserProfile(userId)
  const [text, setText] = useState('')
  const [copied, setCopied] = useState(false)

  const templates = profile.data ? buildMessageTemplates(profile.data) : []

  const pick = (value: string): void => {
    haptic.select()
    setText(value)
    setCopied(false)
  }

  const handleSend = (): void => {
    if (link === null) return

    haptic.tap()
    void navigator.clipboard?.writeText(text).catch(() => undefined)
    setCopied(true)
    openExternalLink(link)
  }

  return (
    <div className="flex min-h-0 flex-col gap-4 overflow-y-auto px-5 pt-5 pb-safe-5">
      <div className="flex flex-col gap-1">
        <SheetTitle className="text-display font-bold">{t('matches.compose.title')}</SheetTitle>
        <SheetDescription className="text-base">
          {t('matches.compose.subtitle', { name })}
        </SheetDescription>
      </div>

      {profile.isPending && <Skeleton className="h-24 w-full rounded-md" />}

      {templates.length > 0 && (
        <section className="flex flex-col gap-2">
          <h3 className="text-tiny tracking-wide text-faint uppercase">
            {t('matches.compose.templatesTitle')}
          </h3>

          {templates.map((template) => {
            const text = describeTemplate(template, t)

            return (
              <button
                key={template.id}
                type="button"
                onClick={() => pick(text)}
                className="flex flex-col items-start gap-1 rounded-md border border-border px-3 py-2.5 text-left transition-colors hover:bg-accent"
              >
                <span className="text-micro tracking-wide text-faint uppercase">
                  {template.kind === 'interest'
                    ? t('matches.compose.anchor.interest')
                    : t('matches.compose.anchor.prompt')}
                </span>
                <span className="text-tiny text-foreground">{text}</span>
              </button>
            )
          })}
        </section>
      )}

      {profile.isSuccess && templates.length === 0 && (
        <p className="text-tiny text-muted-foreground">{t('matches.compose.noTemplates')}</p>
      )}

      <Textarea
        value={text}
        onChange={(event) => setText(event.target.value)}
        placeholder={t('matches.compose.placeholder')}
        rows={4}
      />

      <p className="text-tiny text-faint">{t('matches.compose.rules')}</p>

      <Button size="lg" block disabled={text.trim() === '' || link === null} onClick={handleSend}>
        {copied ? <Check aria-hidden /> : <Copy aria-hidden />}
        {t('matches.compose.copyAndOpen')}
      </Button>

      {link === null && <p className="text-tiny text-destructive">{t('feed.match.noContact')}</p>}
    </div>
  )
}

type Translate = ReturnType<typeof useTranslation>['t']

/** Текст заготовки: ветвим по виду, чтобы типизированный `t` видел свои ключи. */
function describeTemplate(template: MessageTemplate, t: Translate): string {
  return template.kind === 'interest'
    ? t('matches.compose.template.interest', { interest: template.interest })
    : t('matches.compose.template.prompt', { quote: template.quote })
}
