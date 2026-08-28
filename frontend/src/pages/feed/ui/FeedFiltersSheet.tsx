import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useSaveFeedFilters, type FeedFilters } from '@/domains/feed'
import {
  DATING_GOAL_OPTIONS,
  SHOW_GENDER_OPTIONS,
  type DatingGoal,
  type ShowGenderPreference,
} from '@/domains/onboarding'
import { useHaptic } from '@/shared/telegram'
import { Button, Field } from '@/shared/ui'
import { Sheet, SheetContent, SheetTitle } from '@/shared/ui/kit/sheet'
import { Slider } from '@/shared/ui/kit/slider'
import { ToggleGroup } from '@/shared/ui/kit/toggle-group'
import { OptionCard } from '@/shared/ui/OptionCard'
import { RangeField } from '@/shared/ui/RangeField'
import { SegmentedControl } from '@/shared/ui/SegmentedControl'

type FeedFiltersSheetProps = {
  open: boolean
  onClose: () => void
  /** Текущие фильтры с сервера. Форма создаётся уже с ними. */
  filters: FeedFilters
}

const AGE_BOUNDS = { min: 18, max: 80 }
const DISTANCE_BOUNDS = { min: 1, max: 200 }
/** «Активные за неделю» — признак в списке, а на сервере это число дней. */
const ACTIVE_WITHIN_DAYS = 7

/**
 * Требования к анкете — одним списком карточек, как интересы.
 *
 * Раньше это были тумблеры, причём половина пряталась за «Дополнительно»:
 * список читался как настройки приложения, а не как условия подбора, и то,
 * что уже выбрано, приходилось искать построчно. Карточками выбранное видно
 * сразу, а деления на «обычные» и «дополнительные» требования нет — с точки
 * зрения подбора они равнозначны.
 */
const REQUIREMENT_OPTIONS = [
  { value: 'requirePhoto', labelKey: 'feed.filters.requirePhoto', icon: '📷' },
  { value: 'requireFilledProfile', labelKey: 'feed.filters.requireFilledProfile', icon: '📝' },
  { value: 'activeWithinDays', labelKey: 'feed.filters.activeWithinDays', icon: '⚡' },
  { value: 'verifiedOnly', labelKey: 'feed.filters.verifiedOnly', icon: '✅' },
  { value: 'nonSmoker', labelKey: 'feed.filters.nonSmoker', icon: '🚭' },
  { value: 'nonDrinker', labelKey: 'feed.filters.nonDrinker', icon: '🍷' },
  { value: 'noChildren', labelKey: 'feed.filters.noChildren', icon: '👶' },
] as const satisfies ReadonlyArray<{ value: string; labelKey: string; icon: string }>

type Requirement = (typeof REQUIREMENT_OPTIONS)[number]['value']

/**
 * Фильтры подбора (S-15). Правки применяются одной кнопкой: подбор считается
 * серверно, и запрос на каждое движение ползунка был бы расточительным.
 */
export function FeedFiltersSheet({ open, onClose, filters }: FeedFiltersSheetProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const save = useSaveFeedFilters()

  const [draft, setDraft] = useState<FeedFilters>(filters)

  const patch = (changes: Partial<FeedFilters>): void =>
    setDraft((value) => ({ ...value, ...changes }))

  // «Активные за неделю» в фильтрах — число дней, в списке требований — просто
  // отмеченная карточка. Перевод в обе стороны держим здесь.
  const requirements: Requirement[] = REQUIREMENT_OPTIONS.filter(({ value }) =>
    value === 'activeWithinDays' ? draft.activeWithinDays !== null : draft[value],
  ).map(({ value }) => value)

  const handleRequirements = (next: string[]): void => {
    const has = (value: Requirement): boolean => next.includes(value)

    patch({
      requirePhoto: has('requirePhoto'),
      requireFilledProfile: has('requireFilledProfile'),
      verifiedOnly: has('verifiedOnly'),
      nonSmoker: has('nonSmoker'),
      nonDrinker: has('nonDrinker'),
      noChildren: has('noChildren'),
      activeWithinDays: has('activeWithinDays') ? ACTIVE_WITHIN_DAYS : null,
    })
  }

  const handleApply = (): void => {
    haptic.tap()
    save.mutate(draft, { onSuccess: onClose })
  }

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="flex max-h-[92vh] flex-col gap-0 overflow-hidden rounded-t-lg p-0"
      >
        <SheetTitle className="px-5 pt-5 text-display font-bold">
          {t('feed.filters.title')}
        </SheetTitle>

        {/* Скроллится только тело: кнопка «Применить» закреплена снизу, иначе
            на низких экранах она уезжает за край и её нужно искать прокруткой.
            `min-h-0` обязателен — без него потомок flex-колонки не сжимается
            ниже своего контента и вместо прокрутки получается обрезка. */}
        <div className="flex min-h-0 flex-col gap-5 overflow-y-auto p-5">
          <Field label={t('feed.filters.showGender')}>
            <SegmentedControl
              value={draft.showGender}
              onValueChange={(showGender: ShowGenderPreference) => patch({ showGender })}
              label={t('feed.filters.showGender')}
              options={SHOW_GENDER_OPTIONS.map((option) => ({
                value: option.value,
                label: t(option.labelKey),
              }))}
            />
          </Field>

          <Field label={t('feed.filters.age')}>
            <RangeField
              value={[draft.ageRange.min, draft.ageRange.max]}
              onChange={([min, max]) => patch({ ageRange: { min, max } })}
              min={AGE_BOUNDS.min}
              max={AGE_BOUNDS.max}
              suffix={t('onboarding.preferences.ageSuffix')}
              fromLabel={t('onboarding.preferences.ageFromLabel')}
              toLabel={t('onboarding.preferences.ageToLabel')}
            />
          </Field>

          <Field
            label={t('feed.filters.distance')}
            aside={`${draft.maxDistanceKm} ${t('feed.filters.distanceSuffix')}`}
          >
            {/* Без карточки: ползунок расстояния выглядит так же, как ползунок
                возраста выше, — два соседних фильтра не должны различаться
                подложкой. */}
            <Slider
              value={[draft.maxDistanceKm]}
              onValueChange={([maxDistanceKm]) =>
                patch({ maxDistanceKm: maxDistanceKm ?? DISTANCE_BOUNDS.max })
              }
              min={DISTANCE_BOUNDS.min}
              max={DISTANCE_BOUNDS.max}
              step={1}
              aria-label={t('feed.filters.distance')}
              // Тот же отступ до ползунка, что и у возраста (`gap-5`): подпись
              // «Расстояние» — это одновременно и значение справа, и ползунок
              // не должен липнуть к строке со значением.
              className="mt-2.5"
            />
          </Field>

          <Field label={t('feed.filters.goals')}>
            <ToggleGroup
              type="multiple"
              value={draft.datingGoals}
              onValueChange={(datingGoals: string[]) =>
                patch({ datingGoals: datingGoals as DatingGoal[] })
              }
              aria-label={t('feed.filters.goals')}
              spacing={2}
              className="grid w-full grid-cols-2"
            >
              {DATING_GOAL_OPTIONS.map((goal) => (
                <OptionCard
                  key={goal.value}
                  value={goal.value}
                  icon={goal.icon}
                  label={t(goal.labelKey)}
                />
              ))}
            </ToggleGroup>
          </Field>

          <Field label={t('feed.filters.requirements')}>
            <ToggleGroup
              type="multiple"
              value={requirements}
              onValueChange={handleRequirements}
              aria-label={t('feed.filters.requirements')}
              spacing={2}
              className="grid w-full grid-cols-2"
            >
              {REQUIREMENT_OPTIONS.map((option) => (
                <OptionCard
                  key={option.value}
                  value={option.value}
                  icon={option.icon}
                  label={t(option.labelKey)}
                />
              ))}
            </ToggleGroup>
          </Field>
        </div>

        <div className="flex flex-col gap-2 bg-background px-5 pt-4 pb-safe-5">
          {save.isError && (
            <p className="text-center text-tiny text-destructive">{t('feed.filters.saveError')}</p>
          )}

          {/* `flex-1`, а не `block`: `w-full` у второй кнопки в строке
              распирало шторку по горизонтали. */}
          <div className="flex gap-2">
            <Button
              variant="secondary"
              size="lg"
              className="shrink-0"
              onClick={() => setDraft(filters)}
            >
              {t('feed.filters.reset')}
            </Button>
            <Button size="lg" className="flex-1" onClick={handleApply} disabled={save.isPending}>
              {t('feed.filters.apply')}
            </Button>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
