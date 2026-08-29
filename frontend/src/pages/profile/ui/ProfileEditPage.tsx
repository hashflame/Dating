import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate } from '@tanstack/react-router'
import { ArrowLeft, Eye } from 'lucide-react'
import { useCallback, useState, type FormEvent, type ReactNode } from 'react'
import { Controller, useForm, useWatch, type Control } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  getSavedDatePreferences,
  useDatePreferenceCatalog,
  useSaveDatePreferences,
  type DatePreferenceCode,
} from '@/domains/date-preferences'
import { useSaveInterests } from '@/domains/interests'
import { DATING_GOAL_OPTIONS } from '@/domains/onboarding'
import {
  MAX_DATING_GOALS,
  profileFormSchema,
  toProfileForm,
  toProfilePatch,
  useUpdateProfile,
  useViewer,
  useViewerPreview,
  type ProfileFormValues,
  type Viewer,
} from '@/domains/viewer'
import { ROUTES } from '@/shared/config'
import { useFieldError } from '@/shared/i18n'
import { pickQuickQuestions } from '@/shared/lib'
import { useBackButton, useHaptic } from '@/shared/telegram'
import { AutoTextarea, Button, ErrorState, Field, Input, Skeleton } from '@/shared/ui'
import { ToggleGroup } from '@/shared/ui/kit/toggle-group'
import { OptionCard } from '@/shared/ui/OptionCard'
import { InterestPicker, type InterestSelection } from '@/widgets/interest-picker'

import { ProfilePreviewSheet } from './ProfilePreviewSheet'
import { UnsavedChangesSheet } from './UnsavedChangesSheet'

/** Столько интересов принимает бэкенд от одного человека. */
const MAX_INTERESTS = 12

const BIO_MAX = 500
const PROMPT_MAX = 200

/**
 * Варианты привычек ровно те, что принимает бэкенд. Кнопки «—» тут нет
 * намеренно: рядом с «Нет» она читается как второй вариант ответа, хотя
 * означает «не заполнено». Пустое значение показывается тем, что не выбрана
 * ни одна карточка, а снимается повторным тапом.
 */
const HABIT_OPTIONS = [
  { value: 'no', labelKey: 'profile.edit.habitNo' },
  { value: 'sometimes', labelKey: 'profile.edit.habitSometimes' },
  { value: 'regularly', labelKey: 'profile.edit.habitRegularly' },
] as const

/** Без эмодзи: в третью долю ширины иконка и «Жаворонок» вместе не влезают. */
const CHRONOTYPE_OPTIONS = [
  { value: 'earlyBird', labelKey: 'profile.edit.chronotypeEarlyBird' },
  { value: 'nightOwl', labelKey: 'profile.edit.chronotypeNightOwl' },
  { value: 'flexible', labelKey: 'profile.edit.chronotypeFlexible' },
] as const

/** Эмодзи к предпочтениям: сервер отдаёт только название, а в сетке карточек оно теряется. */
const DATE_PREFERENCE_ICONS: Record<DatePreferenceCode, string> = {
  activeOutdoors: '🚵',
  calmHangout: '☕',
  quizzesBoardGames: '🎲',
  somethingNew: '✨',
}

/**
 * Редактирование карточки (S-40).
 *
 * Здесь вся анкета целиком — включая интересы и предпочтения на свидания.
 * Раньше они были ссылками на отдельные экраны, и человек, правя карточку, не
 * видел ни выбранных интересов, ни того, что предпочтения вообще существуют:
 * чтобы их найти, надо было заранее знать, что они есть. Сохранение
 * по-прежнему бьётся на три запроса — таков API, — но кнопка одна, и лишнего
 * не уходит: интересы и предпочтения отправляются, только если их трогали.
 *
 * Экран разбит на смысловые разделы: короткие поля вперемешку с сетками
 * карточек читались как одна бесконечная лента полей.
 */
export function ProfileEditPage() {
  const viewer = useViewer()

  if (viewer.isPending) return <EditSkeleton />
  if (viewer.isError) return <ErrorState onRetry={() => void viewer.refetch()} />

  // Форма создаётся уже с анкетой — reset в эффекте не нужен.
  return <ProfileEditForm viewer={viewer.data} />
}

type ProfileEditFormProps = {
  viewer: Viewer
}

function ProfileEditForm({ viewer }: ProfileEditFormProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const haptic = useHaptic()
  const fieldError = useFieldError()

  // Превью грузим только когда шторку открыли: на входе в форму оно не нужно,
  // а выбранные интересы для формы есть в самой анкете.
  const [previewOpen, setPreviewOpen] = useState(false)
  const [leaveOpen, setLeaveOpen] = useState(false)
  const preview = useViewerPreview(previewOpen)

  const update = useUpdateProfile()
  const saveInterests = useSaveInterests()
  const savePreferences = useSaveDatePreferences()
  const catalog = useDatePreferenceCatalog()

  const { register, control, handleSubmit, formState } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    defaultValues: toProfileForm(viewer),
  })

  // Закреплены навсегда за пользователем (по `viewer.id`) — см. `quick-questions.ts`.
  const quickQuestions = pickQuickQuestions(viewer.id)

  // `null` — не трогали. Отличать это от «выбрал пусто» обязательно: и
  // интересы, и предпочтения сервер заменяет присланным набором целиком, и
  // отправка нетронутого поля стёрла бы сохранённое.
  const [interests, setInterests] = useState<InterestSelection | null>(null)
  const [preferences, setPreferences] = useState<DatePreferenceCode[] | null>(null)

  const selectedInterests: InterestSelection = interests ?? {
    interestIds: viewer.interests.map((interest) => interest.id),
    customInterests: [],
  }

  // Что сохранено, сервер не отдаёт (405, см. docs/api-gaps.md) — берём
  // зеркало последнего сохранения с этого устройства. `null` — не знаем: на
  // другом телефоне или после чистки хранилища честнее сказать об этом, чем
  // показать пустой выбор как достоверный.
  const [savedPreferences] = useState(() => getSavedDatePreferences(viewer.id))
  const selectedPreferences = preferences ?? savedPreferences ?? []

  // Правки живут только в форме: черновика нет, сохранение ручное. Поэтому
  // выход — единственный способ их потерять, и его перехватываем.
  const dirty = formState.isDirty || interests !== null || preferences !== null

  const leave = useCallback(() => void navigate({ to: ROUTES.profile }), [navigate])

  const requestLeave = useCallback(() => {
    if (dirty) {
      setLeaveOpen(true)
      return
    }

    leave()
  }, [dirty, leave])

  useBackButton(requestLeave)

  const saving = update.isPending || saveInterests.isPending || savePreferences.isPending
  const failed = update.isError || saveInterests.isError || savePreferences.isError

  const onSubmit = async (values: ProfileFormValues): Promise<void> => {
    haptic.tap()

    try {
      // Последовательно, а не параллельно: каждый запрос пересчитывает
      // заполненность и может выдать зорки за взятый порог, и считать это
      // одновременно по трём ресурсам — лишний повод для гонки на сервере.
      await update.mutateAsync(toProfilePatch(values))
      if (interests !== null) await saveInterests.mutateAsync(interests)
      if (preferences !== null) {
        await savePreferences.mutateAsync({ userId: viewer.id, preferences })
      }

      haptic.success()
      leave()
    } catch {
      haptic.error()
    }
  }

  return (
    <main className="flex flex-col gap-6 px-4 pt-2">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" aria-label={t('action.back')} onClick={requestLeave}>
          <ArrowLeft aria-hidden />
        </Button>
        <h1 className="text-display font-bold">{t('profile.edit.title')}</h1>
      </div>

      <Section title={t('profile.edit.sectionBasics')}>
        <Field label={t('profile.edit.name')} error={fieldError(formState.errors.name?.message)}>
          <Input {...register('name')} />
        </Field>

        <Field
          label={t('profile.edit.bio')}
          aside={<Counter control={control} name="bio" max={BIO_MAX} />}
          hint={t('profile.edit.bioHint')}
          error={fieldError(formState.errors.bio?.message)}
        >
          <AutoTextarea {...register('bio')} rows={4} maxLength={BIO_MAX} />
        </Field>

        {/* Блок вопросов — единственный внутри секции со своими подзаголовками.
            Без отступа сверху его рубрика липнет к подсказке поля «О себе», и
            две разные группы читаются как одна. */}
        <Field
          label={t('profile.edit.prompts')}
          hint={t('profile.edit.promptsHint')}
          className="mt-2"
        >
          <div className="flex flex-col gap-5">
            {quickQuestions.map((question, index) => (
              <div key={question.id} className="flex flex-col gap-1.5">
                <span className="text-sm font-semibold">{t(question.labelKey)}</span>
                <AutoTextarea {...register(`prompts.${index}`)} rows={2} maxLength={PROMPT_MAX} />
              </div>
            ))}
          </div>
        </Field>
      </Section>

      <Section title={t('profile.edit.sectionHabits')}>
        <Field
          label={t('profile.edit.height')}
          hint={t('profile.edit.heightHint')}
          error={fieldError(formState.errors.height?.message)}
        >
          <div className="relative">
            <Input
              {...register('height')}
              inputMode="numeric"
              maxLength={3}
              onInput={digitsOnly}
              className="pr-12"
            />

            {/* Единица прямо в поле, а не в подсказке под ним: подсказку
                дочитывают уже после того, как ввели неизвестно что. */}
            <span className="pointer-events-none absolute inset-y-0 right-4 flex items-center text-base text-muted-foreground">
              {t('profile.edit.heightUnit')}
            </span>
          </div>
        </Field>

        <HabitField control={control} name="smoking" label={t('profile.edit.smoking')} />
        <HabitField control={control} name="drinking" label={t('profile.edit.drinking')} />

        <Controller
          control={control}
          name="chronotype"
          render={({ field }) => (
            <Field label={t('profile.edit.chronotype')}>
              <ToggleGroup
                type="single"
                value={field.value}
                onValueChange={field.onChange}
                aria-label={t('profile.edit.chronotype')}
                spacing={2}
                className="grid w-full grid-cols-3"
              >
                {CHRONOTYPE_OPTIONS.map((option) => (
                  <OptionCard
                    key={option.value}
                    value={option.value}
                    label={t(option.labelKey)}
                    withCheck={false}
                  />
                ))}
              </ToggleGroup>
            </Field>
          )}
        />
      </Section>

      <Section title={t('profile.edit.sectionGoals')}>
        <Controller
          control={control}
          name="datingGoals"
          render={({ field }) => (
            <Field label={t('profile.edit.datingGoal')} hint={t('profile.edit.datingGoalHint')}>
              <ToggleGroup
                type="multiple"
                value={field.value}
                onValueChange={(next: string[]) => {
                  // Больше двух не даём — то же правило, что и на онбординге (макет S-04).
                  field.onChange(next.slice(-MAX_DATING_GOALS))
                }}
                aria-label={t('profile.edit.datingGoal')}
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
          )}
        />
      </Section>

      <Section title={t('profile.interests')} description={t('profile.edit.interestsHint')}>
        <InterestPicker value={selectedInterests} onChange={setInterests} max={MAX_INTERESTS} />
      </Section>

      <Section title={t('profile.datePrefs')} description={t('profile.datePrefsHint')}>
        {catalog.isPending && <Skeleton className="h-40 w-full rounded-md" />}
        {catalog.isError && <ErrorState onRetry={() => void catalog.refetch()} />}

        {catalog.data && (
          <>
            <ToggleGroup
              type="multiple"
              value={selectedPreferences}
              onValueChange={(next: string[]) => {
                haptic.select()
                setPreferences(next as DatePreferenceCode[])
              }}
              aria-label={t('profile.datePrefs')}
              spacing={2}
              className="grid w-full grid-cols-2"
            >
              {catalog.data.map((preference) => (
                <OptionCard
                  key={preference.id}
                  value={preference.code}
                  label={preference.name}
                  icon={DATE_PREFERENCE_ICONS[preference.code]}
                />
              ))}
            </ToggleGroup>

            {/* Прочитать сохранённый выбор с сервера нечем: `GET
                /api/users/me/date-preferences` отдаёт 405 (docs/api-gaps.md).
                Если зеркала на устройстве нет, список открывается пустым — и
                об этом говорим прямо, а не выдаём пустоту за сохранённое. */}
            {savedPreferences === null && (
              <p className="text-tiny text-faint">{t('profile.datePrefsReplaceWarning')}</p>
            )}
          </>
        )}
      </Section>

      {failed && (
        <p className="text-center text-tiny text-destructive">{t('onboarding.saveError')}</p>
      )}

      {/* Панель липнет к низу: форма длинная, и «Сохранить» не должно ждать
          конца прокрутки. Превью — над сохранением: сначала смотрят, что
          получилось, потом решают сохранять. */}
      <div className="sticky bottom-0 -mx-4 flex flex-col gap-2 bg-background px-4 pt-3 pb-safe-5">
        <Button variant="secondary" size="lg" block onClick={() => setPreviewOpen(true)}>
          <Eye aria-hidden />
          {t('profile.preview')}
        </Button>

        <Button size="lg" block disabled={saving} onClick={() => void handleSubmit(onSubmit)()}>
          {t('action.save')}
        </Button>
      </div>

      <ProfilePreviewSheet
        open={previewOpen}
        onClose={() => setPreviewOpen(false)}
        preview={preview.data}
        isPending={preview.isPending}
        isError={preview.isError}
        onRetry={() => void preview.refetch()}
      />

      <UnsavedChangesSheet open={leaveOpen} onLeave={leave} onStay={() => setLeaveOpen(false)} />
    </main>
  )
}

type SectionProps = {
  title: string
  description?: string
  children: ReactNode
}

/** Смысловой раздел формы: заголовок, необязательное пояснение и поля под ними. */
function Section({ title, description, children }: SectionProps) {
  return (
    <section className="flex flex-col gap-5">
      <div className="flex flex-col gap-1">
        <h2 className="text-lg font-bold text-balance">{title}</h2>
        {description && <p className="text-tiny text-muted-foreground">{description}</p>}
      </div>

      {children}
    </section>
  )
}

type HabitFieldProps = {
  control: Control<ProfileFormValues>
  name: 'smoking' | 'drinking'
  label: string
}

/**
 * Курение и алкоголь — теми же карточками, что интересы и цели. Раньше это был
 * сегментированный переключатель на утопленной дорожке, и он один во всей форме
 * выглядел как настройка приложения, а не как ответ о себе.
 */
function HabitField({ control, name, label }: HabitFieldProps) {
  const { t } = useTranslation()

  return (
    <Controller
      control={control}
      name={name}
      render={({ field }) => (
        <Field label={label}>
          <ToggleGroup
            type="single"
            value={field.value}
            onValueChange={field.onChange}
            aria-label={label}
            spacing={2}
            className="grid w-full grid-cols-3"
          >
            {HABIT_OPTIONS.map((option) => (
              <OptionCard
                key={option.value}
                value={option.value}
                label={t(option.labelKey)}
                withCheck={false}
              />
            ))}
          </ToggleGroup>
        </Field>
      )}
    />
  )
}

type CounterProps = {
  control: Control<ProfileFormValues>
  name: 'bio'
  max: number
}

/**
 * Счётчик символов у подписи поля. Отдельным компонентом, а не `useWatch` в
 * форме: иначе каждая набранная буква перерисовывала бы весь экран целиком.
 */
function Counter({ control, name, max }: CounterProps) {
  const value = useWatch({ control, name })

  return <>{`${value.length}/${max}`}</>
}

/**
 * Не даём набрать в числовое поле буквы: `inputMode` только подсказывает
 * клавиатуру, а вставить или напечатать можно что угодно. Схема всё равно
 * проверяет, но ловить ошибку после сохранения там, где ввод заведомо
 * бессмысленный, — плохой размен.
 */
function digitsOnly(event: FormEvent<HTMLInputElement>): void {
  const input = event.currentTarget
  const cleaned = input.value.replace(/\D/g, '')

  if (cleaned !== input.value) {
    input.value = cleaned
  }
}

function EditSkeleton() {
  return (
    <main className="flex flex-col gap-4 px-4 pt-2">
      <Skeleton className="h-8 w-40" />
      <Skeleton className="h-24 w-full rounded-md" />
      <Skeleton className="h-40 w-full rounded-md" />
    </main>
  )
}
