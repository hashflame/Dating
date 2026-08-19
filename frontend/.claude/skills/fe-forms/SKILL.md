---
name: fe-forms
description: Формы во frontend: react-hook-form + zod, схемы валидации, ключи ошибок вместо текстов, отправка через мутацию, многошаговые формы онбординга, мобильные клавиатуры. Используй, когда делаешь любую форму, поле ввода с валидацией или шаг онбординга. Triggers: form, validation, zod, react-hook-form, input, submit, onboarding step.
---

# Формы

`react-hook-form` + `zod` через `@hookform/resolvers/zod`.

## Схема

Схема живёт рядом с формой, в `model/` слайса:

```ts
// domains/profile/model/profile-form-schema.ts
import { z } from 'zod'

export const profileFormSchema = z.object({
  name: z.string().min(2, 'validation.nameTooShort').max(32, 'validation.nameTooLong'),
  age: z.coerce.number().int().min(18, 'validation.ageMin').max(99, 'validation.ageMax'),
  about: z.string().max(500, 'validation.aboutTooLong').optional(),
})

export type ProfileFormValues = z.infer<typeof profileFormSchema>
```

Правила:

- Тип значений формы всегда получай через `z.infer`, руками не дублируй.
- В сообщениях об ошибке храни **ключ i18n**, а не готовый текст — рендерим через `t()`.
- Одна схема — одна форма. Не переиспользуй схему API-ответа для формы.
- Числа из инпутов — `z.coerce.number()`, иначе придёт строка.

## Форма

```tsx
export function ProfileForm({ onSaved }: ProfileFormProps) {
  const { t } = useTranslation()
  const save = useSaveProfile()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    mode: 'onBlur',
    defaultValues: { name: '', age: 18 },
  })

  const onSubmit = handleSubmit(async (values) => {
    await save.mutateAsync(values)
    onSaved()
  })

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      <Input {...register('name')} aria-invalid={!!errors.name} />
      {errors.name && <p className="text-sm text-destructive">{t(errors.name.message)}</p>}

      <Button type="submit" disabled={isSubmitting} block>
        {t('action.continue')}
      </Button>
    </form>
  )
}
```

- `mode: 'onBlur'` — на мобильном валидация на каждый символ раздражает.
- `defaultValues` задавай всегда, иначе поля становятся неконтролируемыми.
- Кнопку блокируй по `isSubmitting`, а не по своему стейту.
- Ошибку сервера показывай отдельным блоком над кнопкой, не в поле.

## Многошаговые формы (онбординг)

- Каждый шаг — своя схема и свой экран. Валидация шага не ждёт последнего шага.
- Промежуточные значения — zustand-стор с `persist` в `domains/onboarding/model/`,
  чтобы пользователь не терял прогресс при закрытии мини-аппа.
- Отправка на сервер — на каждом шаге, если бэкенд поддерживает черновик;
  иначе одной мутацией в конце.
- Нативная кнопка «Назад» Telegram возвращает на предыдущий шаг — скилл `fe-telegram`.

## Мобильные детали

- Ставь правильный тип клавиатуры: `type="number"`, `inputMode="numeric"`,
  `autoComplete`, `enterKeyHint="next"`.
- Не используй нативные `<select>` для больших списков — лучше лист/шит.
- После ошибки скроллься к первому невалидному полю.

## Чего не делать

- Не валидировать руками в `onChange` — это работа схемы.
- Не дублировать проверки бэкенда как единственную защиту (и наоборот).
- Не хранить значения формы в сторе, пока форма открыта.
