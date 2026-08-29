import { CircleHelp, Link2Off, MessageCircleHeart, PenLine, Quote, Send, Wand2 } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  describeBlockReason,
  messageCharge,
  useMessageLimits,
  useOpenChat,
  type ChatHandoff,
  type MessageBlockReason,
  type MessageKind,
} from '@/domains/messaging'
import { copyToClipboard } from '@/shared/lib'
import { openTelegramChat, useHaptic } from '@/shared/telegram'
import { AutoTextarea, Button } from '@/shared/ui'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/shared/ui/kit/sheet'
import { ComingSoon } from '@/widgets/coming-soon'
import { MessageCostLine, MessageLimitSheet } from '@/widgets/message-limits'

/** Длиннее первое сообщение не нужно: два-три предложения работают лучше. */
const MAX_LENGTH = 500

/** Причина отказа → текст. Ветвим списком, чтобы типизированный `t` видел ключи. */
const REASON_KEYS = {
  blocksMessages: 'messages.blocked.blocksMessages',
  deleted: 'messages.blocked.deleted',
  blocked: 'messages.blocked.blocked',
  noUsername: 'messages.blocked.noUsername',
  noSparks: 'messages.blocked.noSparks',
  unknown: 'messages.blocked.unknown',
} as const

type ComposeSheetProps = {
  open: boolean
  onClose: () => void
  /** С кем открываем переписку. */
  userId: string
  name: string
  /** Обычное сообщение уходит мэтчу, суперсообщение — кому угодно из ленты. */
  kind: MessageKind
  /** Есть только у сообщения мэтчу: по нему сервер находит пару. */
  matchId?: string
  /** Переход состоялся: экран решает сам, закрыть шторку или листать деку. */
  onOpened?: (result: ChatHandoff) => void
}

/**
 * Первое сообщение (S-33) и суперсообщение.
 *
 * Приложение сообщения не доставляет: человек составляет текст здесь, а пишет
 * в обычной личке Telegram. Кнопка забирает текст в буфер и открывает чат —
 * дальше остаётся вставить. Платится не доставка, а переход сверх недельного
 * лимита.
 *
 * Сверху — AI-подсказка, её ещё нет, и об этом сказано прямо: место под неё
 * занято свёрнутой карточкой «в разработке», а не пустотой, чтобы человек знал,
 * что готовится, и не искал кнопку. Ниже — правила, по которым первое сообщение
 * вообще срабатывает, а под ними поле и кнопка перехода: то, что делают руками,
 * стоит рядом, подсказки — до него.
 */
export function ComposeSheet({
  open,
  onClose,
  userId,
  name,
  kind,
  matchId,
  onOpened,
}: ComposeSheetProps) {
  const { t } = useTranslation()

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex max-h-[92vh] flex-col gap-0 overflow-hidden rounded-t-xl border-0 p-0"
      >
        {open && (
          <ComposeBody
            userId={userId}
            name={name}
            kind={kind}
            matchId={matchId}
            onOpened={onOpened}
            onClose={onClose}
          />
        )}
      </SheetContent>
    </Sheet>
  )
}

type ComposeBodyProps = {
  userId: string
  name: string
  kind: MessageKind
  matchId?: string
  onOpened?: (result: ChatHandoff) => void
  onClose: () => void
}

/**
 * Отдельный компонент: черновик должен начинаться заново при каждом открытии,
 * а сама шторка остаётся смонтированной ради анимации закрытия.
 */
function ComposeBody({ userId, name, kind, matchId, onOpened, onClose }: ComposeBodyProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()

  const limits = useMessageLimits()
  const openChat = useOpenChat()

  const [text, setText] = useState('')
  /** Почему перейти не вышло. `null` — пока не пробовали или всё в порядке. */
  const [blocked, setBlocked] = useState<MessageBlockReason | null>(null)
  /** Лимит исчерпан и мы спрашиваем согласие на списание. */
  const [confirming, setConfirming] = useState<MessageKind | null>(null)

  const charge = limits.data ? messageCharge(limits.data, kind) : null
  const isSuper = kind === 'super'

  const submit = (): void => {
    setBlocked(null)
    openChat.mutate(
      { userId, kind, text: text.trim(), matchId },
      {
        onSuccess: (result) => {
          haptic.success()
          setConfirming(null)
          // Текст в буфер: вставить его в чат человек должен сам — Telegram не
          // даёт заполнить поле ввода чужого диалога за пользователя.
          copyToClipboard(text.trim())
          openTelegramChat(result.chatUrl)
          onOpened?.(result)
          onClose()
        },
        onError: (reason) => {
          haptic.error()
          setConfirming(null)
          setBlocked(describeBlockReason(reason))
        },
      },
    )
  }

  const handleOpen = (): void => {
    haptic.tap()

    // Бесплатный переход делаем сразу, платный — только после согласия с ценой.
    if (charge !== null && !charge.free) {
      setConfirming(kind)
      return
    }

    submit()
  }

  return (
    <div className="flex min-h-0 flex-col gap-5 overflow-y-auto px-5 pt-5 pb-safe-5">
      <div className="flex flex-col gap-1">
        <SheetTitle className="flex items-center gap-2 text-display font-bold">
          {isSuper && <MessageCircleHeart className="size-5 shrink-0 text-brand" aria-hidden />}
          {isSuper ? t('messages.compose.superTitle') : t('messages.compose.title')}
        </SheetTitle>

        <SheetDescription className="text-base">
          {isSuper
            ? t('messages.compose.superSubtitle', { name })
            : t('messages.compose.subtitle', { name })}
        </SheetDescription>
      </div>

      <ComingSoon
        compact
        collapsible
        title={t('messages.ai.title')}
        description={t('messages.ai.description')}
        points={[
          { icon: Wand2, title: t('messages.ai.byProfile'), text: t('messages.ai.byProfileText') },
          { icon: Quote, title: t('messages.ai.variants'), text: t('messages.ai.variantsText') },
          { icon: PenLine, title: t('messages.ai.tone'), text: t('messages.ai.toneText') },
        ]}
      />

      <Tips />

      <section className="flex flex-col gap-2">
        <h3 className="text-eyebrow font-bold text-muted-foreground uppercase">
          {t('messages.compose.ownTitle')}
        </h3>

        <AutoTextarea
          value={text}
          onChange={(event) => setText(event.target.value)}
          placeholder={t('messages.compose.placeholder')}
          maxLength={MAX_LENGTH}
          rows={4}
        />
      </section>

      <div className="flex flex-col gap-2">
        {charge !== null && <MessageCostLine charge={charge} />}

        <Button
          size="lg"
          block
          disabled={text.trim() === '' || charge === null || openChat.isPending}
          onClick={handleOpen}
        >
          <Send aria-hidden />
          {charge !== null && !charge.free
            ? t('messages.compose.openFor', { cost: charge.cost })
            : t('messages.compose.open')}
        </Button>

        <p className="text-center text-tiny text-faint">{t('messages.compose.handoffHint')}</p>

        {blocked !== null && (
          <p className="flex items-start gap-1.5 rounded-md bg-destructive/10 px-3 py-2 text-tiny text-destructive">
            <Link2Off className="mt-0.5 size-3.5 shrink-0" aria-hidden />
            {t(REASON_KEYS[blocked], { name })}
          </p>
        )}
      </div>

      {charge !== null && (
        <MessageLimitSheet
          kind={confirming}
          charge={charge}
          sparksBalance={limits.data?.sparksBalance ?? 0}
          pending={openChat.isPending}
          onConfirm={submit}
          onClose={() => setConfirming(null)}
        />
      )}
    </div>
  )
}

/**
 * Правила первого сообщения. Раньше это была одна строка мелким шрифтом — её
 * не читали. Три пункта с примерами читаются: каждый можно применить сразу,
 * не переводя совет на язык действий.
 *
 * Стоят над полем и без заливки: карточка на `--surface` в шторке читалась
 * ровно как поле ввода, и подсказки принимали за второй инпут. Под полем
 * оставалось только то, что нажимают — сам ввод и кнопка перехода.
 */
function Tips() {
  const { t } = useTranslation()

  return (
    <ul className="flex flex-col gap-2">
      <Tip icon={Quote} title={t('messages.tips.anchor')} text={t('messages.tips.anchorText')} />
      <Tip
        icon={CircleHelp}
        title={t('messages.tips.question')}
        text={t('messages.tips.questionText')}
      />
      <Tip icon={PenLine} title={t('messages.tips.short')} text={t('messages.tips.shortText')} />
    </ul>
  )
}

type TipProps = {
  icon: typeof Quote
  title: string
  text: string
}

function Tip({ icon: Icon, title, text }: TipProps) {
  return (
    <li className="flex items-start gap-2">
      <Icon className="mt-0.5 size-3.5 shrink-0 text-brand" aria-hidden />

      <span className="flex flex-col">
        <span className="text-tiny font-semibold text-foreground">{title}</span>
        <span className="text-tiny text-muted-foreground">{text}</span>
      </span>
    </li>
  )
}
