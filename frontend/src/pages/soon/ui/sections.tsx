import { Lightbulb } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { SoonScreen } from './SoonScreen'

/** Доска идей по продукту (S-60): эндпоинтов нет совсем (T-19.1).
    Идеи свидания — это другое, они живут в хабе мэтча. */
export function IdeasPage() {
  const { t } = useTranslation()

  return <SoonScreen icon={Lightbulb} title={t('soon.ideas')} description={t('soon.description')} />
}
