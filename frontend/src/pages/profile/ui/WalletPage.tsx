import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft, ChevronRight, Star } from 'lucide-react'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import {
  useSparksWallet,
  type SparkEarnOption,
  type SparkTransaction,
  type SparkTransactionType,
} from '@/domains/sparks'
import { ROUTES } from '@/shared/config'
import { useBackButton } from '@/shared/telegram'
import { Button, Card, EmptyState, ErrorState, ListRow, Skeleton } from '@/shared/ui'

/**
 * Типы операций сервер отдаёт кодами без подписей, поэтому названия живут тут.
 * Незнакомый код — показываем сам код: молчать о движении зорок нельзя.
 */
const TYPE_KEYS = {
  registrationBonus: 'profile.spark.registrationBonus',
  profileCompletion: 'profile.spark.profileCompletion',
  verification: 'profile.spark.verification',
  referral: 'profile.spark.referral',
  ideaSubmission: 'profile.spark.ideaSubmission',
  ideaImplemented: 'profile.spark.ideaImplemented',
  contactUnlock: 'profile.spark.contactUnlock',
  likesReveal: 'profile.spark.likesReveal',
  purchase: 'profile.spark.purchase',
  refund: 'profile.spark.refund',
  devReset: 'profile.spark.devReset',
  accountRevival: 'profile.spark.accountRevival',
} as const

/** Кошелёк зорок (S-46): баланс, за что дают и история операций. */
export function WalletPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const wallet = useSparksWallet()
  const goBack = useCallback(() => void navigate({ to: ROUTES.profile }), [navigate])
  useBackButton(goBack)

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('profile.wallet')}</h1>
      </div>

      {wallet.isPending && <Skeleton className="h-64 w-full rounded-md" />}
      {wallet.isError && <ErrorState onRetry={() => void wallet.refetch()} />}

      {wallet.data && (
        <>
          <Card padding="tight" className="flex flex-col items-center gap-1 py-6">
            <span className="flex items-center gap-2">
              <Star className="size-7 text-brand" aria-hidden />
              <span className="text-display font-bold">{wallet.data.balance}</span>
            </span>
            <span className="text-tiny text-muted-foreground">{t('profile.walletHint')}</span>
          </Card>

          <section className="flex flex-col gap-1.5">
            <h2 className="text-tiny tracking-wide text-faint uppercase">
              {t('profile.earnTitle')}
            </h2>

            {/* Каталог показываем как есть, включая уже полученное: у строки
                теперь честная подпись «получено» или прогресс, так что список
                читается как состояние, а не как справка. */}
            <Card padding="none" className="overflow-hidden">
              {wallet.data.earnOptions.map((option) => (
                <EarnRow
                  key={option.type}
                  option={option}
                  onClick={
                    option.type === 'referral'
                      ? () => void navigate({ to: ROUTES.profileInvite })
                      : undefined
                  }
                />
              ))}
            </Card>
          </section>

          <section className="flex flex-col gap-1.5">
            <h2 className="text-tiny tracking-wide text-faint uppercase">
              {t('profile.historyTitle')}
            </h2>

            {wallet.data.history.items.length === 0 ? (
              <EmptyState icon={Star} title={t('profile.historyEmpty')} />
            ) : (
              <Card padding="none" className="overflow-hidden">
                {wallet.data.history.items.map((item) => (
                  <HistoryRow key={item.id} item={item} />
                ))}
              </Card>
            )}

            {wallet.data.history.hasMore && (
              <p className="text-tiny text-muted-foreground">
                {t('profile.historyMore', { count: wallet.data.history.totalCount })}
              </p>
            )}
          </section>
        </>
      )}
    </main>
  )
}

type EarnRowProps = {
  option: SparkEarnOption
  /** Строка ведёт дальше — тогда у неё шеврон, как на макете S-46. */
  onClick?: () => void
}

/**
 * Строка «как заработать». Название берём из i18n, а не из `option.label`:
 * сервер локализует его по языку Telegram-профиля, а интерфейс может быть на
 * другом — по той же причине не используется `nextReward.hint`. Прогресс и
 * «уже получено» — наоборот, серверные: клиенту их неоткуда взять.
 */
function EarnRow({ option, onClick }: EarnRowProps) {
  const { t } = useTranslation()

  // Порог 1 — это «да/нет» (верификация, бонус за регистрацию): «0 из 1»
  // ничего не добавляет, там всё говорит само «получено» или его отсутствие.
  const hasProgress = option.progress !== null && option.threshold !== null && option.threshold > 1

  let subtitle: string | undefined
  if (option.completed) {
    subtitle = t('profile.earnDone')
  } else if (hasProgress) {
    subtitle = t('profile.earnProgress', { progress: option.progress, threshold: option.threshold })
  }

  return (
    <ListRow
      title={describeType(option.type, t)}
      subtitle={subtitle}
      trailing={
        <span className="flex items-center gap-1">
          <span className={option.completed ? 'text-faint' : 'text-moss'}>
            {t('profile.earnAmount', { amount: option.amount })}
          </span>
          {onClick && <ChevronRight className="size-4 text-faint" aria-hidden />}
        </span>
      }
      onClick={onClick}
    />
  )
}

type HistoryRowProps = {
  item: SparkTransaction
}

function HistoryRow({ item }: HistoryRowProps) {
  const { t, i18n } = useTranslation()

  const date = new Intl.DateTimeFormat(i18n.language, { day: 'numeric', month: 'long' }).format(
    new Date(item.createdAt),
  )

  return (
    <ListRow
      title={describeType(item.type, t)}
      subtitle={date}
      trailing={
        <span className={item.amount < 0 ? 'text-muted-foreground' : 'text-moss'}>
          {item.amount > 0 ? `+${String(item.amount)}` : item.amount}
        </span>
      }
    />
  )
}

function describeType(
  type: SparkTransactionType,
  t: (key: (typeof TYPE_KEYS)[keyof typeof TYPE_KEYS]) => string,
): string {
  const key = TYPE_KEYS[type]

  return key === undefined ? type : t(key)
}
