import { Lightbulb, Sparkles, UserRound } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { SoonScreen } from './SoonScreen'

/** Мэтчи: список есть, а хаб мэтча на стенде отдаёт 404. */
export function MatchesPage() {
  const { t } = useTranslation()

  return (
    <SoonScreen icon={Sparkles} title={t('soon.matches')} description={t('soon.description')} />
  )
}

/** Идеи свиданий: эндпоинтов нет совсем. */
export function IdeasPage() {
  const { t } = useTranslation()

  return <SoonScreen icon={Lightbulb} title={t('soon.ideas')} description={t('soon.description')} />
}

/** Карточка профиля: эндпоинтов профиля нет. */
export function ProfilePage() {
  const { t } = useTranslation()

  return (
    <SoonScreen icon={UserRound} title={t('soon.profile')} description={t('soon.description')} />
  )
}
