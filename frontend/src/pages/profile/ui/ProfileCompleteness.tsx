import { Check, ChevronDown } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { type Viewer } from '@/domains/viewer'
import { cn } from '@/shared/lib'
import { Card, ProgressBar } from '@/shared/ui'

/** Сколько нужно, чтобы засчитали пункт. Сверено с `ProfileCompletenessCalculator`. */
const MIN_PHOTOS = 3
const MIN_INTERESTS = 5

type ProfileCompletenessProps = {
  viewer: Viewer
}

/**
 * Заполненность карточки (S-40) с разбором по пунктам.
 *
 * Без разбора экран врал: человек заполнял «о себе», рост и привычки, а процент
 * не двигался — эти поля в формулу не входят вовсе (`ProfileCompletenessCalculator`:
 * 35% база плюс фото/интересы/ответы/предпочтения/верификация/голос/Instagram).
 * Поэтому список свёрнут по умолчанию, но раскрывается и показывает, что именно
 * осталось и сколько это даст.
 */
export function ProfileCompleteness({ viewer }: ProfileCompletenessProps) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)

  const items = [
    {
      key: 'photos' as const,
      label: t('profile.completenessItem.photos', { count: MIN_PHOTOS }),
      progress: t('profile.completenessItem.photosProgress', {
        current: viewer.photos.length,
        total: MIN_PHOTOS,
      }),
      points: 15,
      done: viewer.photos.length >= MIN_PHOTOS,
    },
    {
      key: 'interests' as const,
      label: t('profile.completenessItem.interests', { count: MIN_INTERESTS }),
      progress: t('profile.completenessItem.interestsProgress', {
        current: viewer.interests.length,
        total: MIN_INTERESTS,
      }),
      points: 10,
      done: viewer.interests.length >= MIN_INTERESTS,
    },
    {
      key: 'prompts' as const,
      label: t('profile.completenessItem.prompts'),
      progress: undefined,
      points: 10,
      done: viewer.prompts.length > 0,
    },
    {
      key: 'instagram' as const,
      label: t('profile.completenessItem.instagram'),
      progress: undefined,
      points: 5,
      done: viewer.instagramHandle !== null,
    },
    {
      key: 'voice' as const,
      label: t('profile.completenessItem.voice'),
      progress: t('profile.completenessItem.notAvailable'),
      points: 5,
      done: viewer.voiceIntroUrl !== null,
    },
    {
      key: 'verification' as const,
      label: t('profile.completenessItem.verification'),
      progress: t('profile.completenessItem.notAvailable'),
      points: 10,
      done: viewer.isVerified,
    },
  ]

  return (
    <Card padding="tight" className="flex flex-col gap-2">
      <span className="flex items-baseline justify-between gap-2">
        <span className="text-base font-semibold">{t('profile.completeness')}</span>
        <span className="text-tiny text-muted-foreground">{viewer.profileCompleteness}%</span>
      </span>

      <ProgressBar value={viewer.profileCompleteness} />

      {viewer.nextReward && (
        <span className="text-tiny text-muted-foreground">
          {t('profile.nextReward', {
            threshold: viewer.nextReward.threshold,
            reward: viewer.nextReward.sparksReward,
          })}
        </span>
      )}

      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        className="flex min-h-9 items-center justify-between gap-2 text-left text-tiny text-brand"
      >
        {open ? t('profile.completenessHide') : t('profile.completenessShow')}
        <ChevronDown
          className={cn('size-4 transition-transform duration-150', open && 'rotate-180')}
          aria-hidden
        />
      </button>

      {open && (
        <div className="flex flex-col gap-2">
          <p className="text-tiny text-muted-foreground">{t('profile.completenessWhy')}</p>

          <ul className="flex flex-col gap-1.5">
            {items.map((item) => (
              <li key={item.key} className="flex items-start gap-2">
                <span
                  className={cn(
                    'mt-0.5 flex size-4 shrink-0 items-center justify-center rounded-full',
                    item.done ? 'bg-brand text-brand-foreground' : 'bg-tag',
                  )}
                  aria-hidden
                >
                  {item.done && <Check className="size-3" />}
                </span>

                <span className="flex min-w-0 flex-1 flex-col">
                  <span className={cn('text-tiny', item.done ? 'text-faint' : 'text-foreground')}>
                    {item.label}
                  </span>
                  {!item.done && item.progress !== undefined && (
                    <span className="text-micro text-faint">{item.progress}</span>
                  )}
                </span>

                <span className={cn('text-tiny', item.done ? 'text-faint' : 'text-moss')}>
                  +{item.points}%
                </span>
              </li>
            ))}
          </ul>

          <p className="text-tiny text-faint">{t('profile.completenessNote')}</p>
        </div>
      )}
    </Card>
  )
}
