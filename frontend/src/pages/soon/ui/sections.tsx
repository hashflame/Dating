import { Lightbulb } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { SoonScreen } from './SoonScreen'

/** Идеи свиданий: эндпоинтов нет совсем. */
export function IdeasPage() {
  const { t } = useTranslation()

  return <SoonScreen icon={Lightbulb} title={t('soon.ideas')} description={t('soon.description')} />
}
