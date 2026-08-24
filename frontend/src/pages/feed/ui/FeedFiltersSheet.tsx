import { ChevronDown } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { useSaveFeedFilters, type FeedFilters } from '@/domains/feed'
import {
  DATING_GOAL_OPTIONS,
  SHOW_GENDER_OPTIONS,
  type DatingGoal,
  type ShowGenderPreference,
} from '@/domains/onboarding'
import { cn } from '@/shared/lib'
import { useHaptic } from '@/shared/telegram'
import { Button, Field } from '@/shared/ui'
import { Sheet, SheetContent, SheetTitle } from '@/shared/ui/kit/sheet'
import { Slider } from '@/shared/ui/kit/slider'
import { ToggleGroup } from '@/shared/ui/kit/toggle-group'
import { OptionCard } from '@/shared/ui/OptionCard'
import { RangeField } from '@/shared/ui/RangeField'
import { SegmentedControl } from '@/shared/ui/SegmentedControl'
import { SwitchRow } from '@/shared/ui/SwitchRow'

type FeedFiltersSheetProps = {
  open: boolean
  onClose: () => void
  /** Текущие фильтры с сервера. Форма создаётся уже с ними. */
  filters: FeedFilters
}

const AGE_BOUNDS = { min: 18, max: 80 }
const DISTANCE_BOUNDS = { min: 1, max: 200 }
/** «Активные за неделю» — тумблер, а на сервере это число дней. */
const ACTIVE_WITHIN_DAYS = 7

/**
 * Фильтры подбора (S-15). Правки применяются одной кнопкой: подбор считается
 * серверно, и запрос на каждое движение ползунка был бы расточительным.
 */
export function FeedFiltersSheet({ open, onClose, filters }: FeedFiltersSheetProps) {
  const { t } = useTranslation()
  const haptic = useHaptic()
  const save = useSaveFeedFilters()

  const [draft, setDraft] = useState<FeedFilters>(filters)
  const [advancedOpen, setAdvancedOpen] = useState(false)

  const patch = (changes: Partial<FeedFilters>): void =>
    setDraft((value) => ({ ...value, ...changes }))

  const handleApply = (): void => {
    haptic.tap()
    save.mutate(draft, { onSuccess: onClose })
  }

  return (
    <Sheet open={open} onOpenChange={(next) => !next && onClose()}>
      <SheetContent
        side="bottom"
        closeLabel={t('action.close')}
        className="max-h-[92vh] gap-0 overflow-y-auto rounded-t-xl p-0"
      >
        <SheetTitle className="px-5 pt-5 text-display font-bold">
          {t('feed.filters.title')}
        </SheetTitle>

        <div className="flex flex-col gap-5 p-5">
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
            <Slider
              value={[draft.maxDistanceKm]}
              onValueChange={([maxDistanceKm]) =>
                patch({ maxDistanceKm: maxDistanceKm ?? DISTANCE_BOUNDS.max })
              }
              min={DISTANCE_BOUNDS.min}
              max={DISTANCE_BOUNDS.max}
              step={1}
              aria-label={t('feed.filters.distance')}
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

          <div className="flex flex-col">
            <SwitchRow
              label={t('feed.filters.requirePhoto')}
              checked={draft.requirePhoto}
              onCheckedChange={(requirePhoto) => patch({ requirePhoto })}
            />
            <SwitchRow
              label={t('feed.filters.requireFilledProfile')}
              checked={draft.requireFilledProfile}
              onCheckedChange={(requireFilledProfile) => patch({ requireFilledProfile })}
            />
            <SwitchRow
              label={t('feed.filters.activeWithinDays')}
              checked={draft.activeWithinDays !== null}
              onCheckedChange={(active) =>
                patch({ activeWithinDays: active ? ACTIVE_WITHIN_DAYS : null })
              }
            />
          </div>

          <div className="flex flex-col">
            <button
              type="button"
              onClick={() => setAdvancedOpen((value) => !value)}
              aria-expanded={advancedOpen}
              className="flex min-h-11 items-center justify-between gap-2 text-base font-semibold outline-none"
            >
              {t('feed.filters.advanced')}
              <ChevronDown
                className={cn('size-4 transition-transform', advancedOpen && 'rotate-180')}
                aria-hidden
              />
            </button>

            {advancedOpen && (
              <div className="flex flex-col">
                <SwitchRow
                  label={t('feed.filters.verifiedOnly')}
                  checked={draft.verifiedOnly}
                  onCheckedChange={(verifiedOnly) => patch({ verifiedOnly })}
                />
                <SwitchRow
                  label={t('feed.filters.nonSmoker')}
                  checked={draft.nonSmoker}
                  onCheckedChange={(nonSmoker) => patch({ nonSmoker })}
                />
                <SwitchRow
                  label={t('feed.filters.nonDrinker')}
                  checked={draft.nonDrinker}
                  onCheckedChange={(nonDrinker) => patch({ nonDrinker })}
                />
                <SwitchRow
                  label={t('feed.filters.noChildren')}
                  checked={draft.noChildren}
                  onCheckedChange={(noChildren) => patch({ noChildren })}
                />
              </div>
            )}
          </div>

          {save.isError && (
            <p className="text-center text-tiny text-destructive">{t('feed.filters.saveError')}</p>
          )}

          <div className="flex gap-2 pb-safe">
            <Button variant="secondary" size="lg" onClick={() => setDraft(filters)}>
              {t('feed.filters.reset')}
            </Button>
            <Button size="lg" block onClick={handleApply} disabled={save.isPending}>
              {t('feed.filters.apply')}
            </Button>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
