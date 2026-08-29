import { useNavigate, useParams } from '@tanstack/react-router'
import { ArrowLeft, Images, MapPin, Sparkles, Wallet } from 'lucide-react'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'

import { ROUTES } from '@/shared/config'
import { useBackButton } from '@/shared/telegram'
import { Button } from '@/shared/ui'
import { ComingSoon } from '@/widgets/coming-soon'

/**
 * Идея свидания (S-39) — ветка хаба мэтча, сейчас в разработке.
 *
 * Прошлый вариант подбирал формат из фиксированного каталога («прогулка»,
 * «кофе») по пересечению предпочтений: разным парам приходило одно и то же, и
 * решить, куда именно пойти, это не помогало. Экран честно погашен, пока
 * бэкенд не научится собирать конкретное место с фото и деталями (тикеты
 * заведены) — но вместо выключенной строки в хабе показываем, что готовится.
 *
 * Кнопки «Мы договорились о встрече» здесь больше нет: она отмечала успех
 * подбора, которого пока не существует.
 */
export function DateIdeaPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { matchId } = useParams({ from: ROUTES.matchDateIdea })

  const goBack = useCallback(
    () => void navigate({ to: ROUTES.matchHub, params: { matchId } }),
    [navigate, matchId],
  )
  useBackButton(goBack)

  return (
    <main className="flex flex-col gap-4 px-4 pt-2 pb-safe-5">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={goBack}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('matches.dateIdea.title')}</h1>
      </div>

      <ComingSoon
        title={t('matches.dateIdea.soon.title')}
        description={t('matches.dateIdea.soon.description')}
        points={[
          {
            icon: MapPin,
            title: t('matches.dateIdea.soon.place'),
            text: t('matches.dateIdea.soon.placeText'),
          },
          {
            icon: Images,
            title: t('matches.dateIdea.soon.photo'),
            text: t('matches.dateIdea.soon.photoText'),
          },
          {
            icon: Wallet,
            title: t('matches.dateIdea.soon.details'),
            text: t('matches.dateIdea.soon.detailsText'),
          },
          {
            icon: Sparkles,
            title: t('matches.dateIdea.soon.personal'),
            text: t('matches.dateIdea.soon.personalText'),
          },
        ]}
      />
    </main>
  )
}
