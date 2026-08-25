import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft, Star } from 'lucide-react'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import { useSparksWallet, type SparkTransaction, type SparkTransactionType } from '@/domains/sparks'
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
  superlike: 'profile.spark.superlike',
  likesReveal: 'profile.spark.likesReveal',
  purchase: 'profile.spark.purchase',
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

            {/* Показываем каталог как есть, включая одноразовый бонус за
                регистрацию: признака «уже получено» в ответе нет, и решать за
                сервер, что пользователю не покажут, — не наше дело. */}
            <Card padding="none" className="overflow-hidden">
              {wallet.data.earnOptions.map((option) => (
                <ListRow
                  key={option.type}
                  title={describeType(option.type, t)}
                  subtitle={t('profile.earnAmount', { amount: option.amount })}
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
