import { useRouter } from '@tanstack/react-router'
import { ArrowLeft } from 'lucide-react'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import { useBackButton, useHaptic } from '@/shared/telegram'
import { Button, Card } from '@/shared/ui'

type LegalDocument = 'terms' | 'privacy'

type LegalPageProps = {
  document: LegalDocument
}

/**
 * Правила и Политика (открываются со экрана согласия).
 *
 * Документы живут внутри мини-аппа, а не на сайте: своего домена у продукта
 * пока нет, а `openLink` увёл бы человека из приложения посреди регистрации.
 * Тексты — черновые, см. docs/api-gaps.md.
 */
export function LegalPage({ document }: LegalPageProps) {
  const { t } = useTranslation()
  const router = useRouter()
  const haptic = useHaptic()

  const goBack = useCallback(() => {
    haptic.tap()
    router.history.back()
  }, [haptic, router])

  useBackButton(goBack)

  return (
    <main className="flex flex-1 flex-col gap-4 px-5 pt-4 pb-8">
      <div className="flex items-center gap-1">
        <Button
          variant="ghost"
          size="icon"
          onClick={goBack}
          aria-label={t('action.back')}
          className="border-0"
        >
          <ArrowLeft aria-hidden />
        </Button>

        <h1 className="text-lg font-bold">{t(`legal.${document}.title`)}</h1>
      </div>

      <Card padding="tight" className="text-tiny text-muted-foreground">
        {t('legal.draftNotice')}
      </Card>

      <p className="text-base whitespace-pre-line text-foreground">{t(`legal.${document}.body`)}</p>
    </main>
  )
}
