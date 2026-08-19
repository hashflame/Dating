import { useTranslation } from 'react-i18next'

import { ViewerBalance } from '@/domains/viewer'
import { Button } from '@/shared/ui'

/** Стартовый экран. Заглушка — заменяется первой реальной story. */
export function HomePage() {
  const { t } = useTranslation()

  return (
    <main className="flex flex-1 flex-col items-center justify-center gap-6 px-6 text-center">
      <h1 className="text-2xl font-semibold">{t('app.name')}</h1>
      <ViewerBalance />
      <Button variant="brand" size="lg" block>
        {t('action.continue')}
      </Button>
    </main>
  )
}
