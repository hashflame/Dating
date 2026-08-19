import { useTranslation } from 'react-i18next'

import { cn } from '@/shared/lib'

/** Задержки делят цикл `--animate-hero-slide` (10.2 с) на три равные части. */
const SLIDES = [
  {
    titleKey: 'welcome.hero.interestsTitle',
    subtitleKey: 'welcome.hero.interestsSubtitle',
    delay: '0ms',
  },
  {
    titleKey: 'welcome.hero.nearbyTitle',
    subtitleKey: 'welcome.hero.nearbySubtitle',
    delay: '3400ms',
  },
  {
    titleKey: 'welcome.hero.seriousTitle',
    subtitleKey: 'welcome.hero.seriousSubtitle',
    delay: '6800ms',
  },
] as const

/**
 * Шапка приветствия: три сменяющихся примера того, по чему идёт подбор.
 * Белый текст здесь не нарушает правило токенов — подложка фиксированно тёмная.
 * При `prefers-reduced-motion` остаётся первый слайд.
 */
export function WelcomeHero() {
  const { t } = useTranslation()

  return (
    <div className="relative h-[150px] overflow-hidden rounded-lg text-white bg-gradient-photo-1">
      {SLIDES.map((slide, index) => (
        <div
          key={slide.titleKey}
          className={cn(
            'absolute inset-0 flex flex-col items-center justify-center px-5 text-center opacity-0',
            'motion-safe:animate-hero-slide',
            index === 0 ? 'motion-reduce:opacity-100' : 'motion-reduce:hidden',
          )}
          style={{ animationDelay: slide.delay }}
        >
          <p className="text-xl font-bold">{t(slide.titleKey)}</p>
          <p className="mt-1 text-tiny opacity-85">{t(slide.subtitleKey)}</p>
        </div>
      ))}

      <div className="absolute bottom-3 flex w-full justify-center gap-1.5" aria-hidden>
        {SLIDES.map((slide, index) => (
          <span
            key={slide.titleKey}
            className={cn(
              'h-[5px] w-[5px] rounded-full bg-white/35',
              'motion-safe:animate-hero-dot',
              index === 0 && 'motion-reduce:w-3.5 motion-reduce:bg-white',
            )}
            style={{ animationDelay: slide.delay }}
          />
        ))}
      </div>
    </div>
  )
}
