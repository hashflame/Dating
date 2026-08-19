---
name: fe-styles-refactor
description: Рефакторинг разъехавшейся верстки во frontend: замена хардкода цветов на токены, вынос вариантов в cva, дедупликация повторяющихся наборов классов, разбор длинных className, чистка инлайн-стилей. Используй, когда стили дублируются, появились произвольные цвета и magic-числа, или нужно привести экраны к общему виду. Triggers: refactor styles, cleanup CSS, duplicate classes, cva, design consistency.
---

# Рефакторинг стилей

Рефакторинг стилей — отдельная задача. Не меняй в ней поведение компонентов.

## Порядок работы

1. Собери, что чинить: `grep` по проблемным паттернам (ниже).
2. Чини по одному типу проблем за проход.
3. После каждого прохода — `npm run lint && npm run build`.
4. Визуально сверься: экран должен выглядеть так же, если задача не про смену вида.

## Что искать

```bash
cd frontend
grep -rn 'bg-\[#\|text-\[#\|border-\[#' src            # произвольные цвета
grep -rn 'bg-white\|bg-black\|text-white\|text-gray' src  # цвета вне токенов
grep -rn 'dark:' src                                   # ручной dark mode
grep -rn 'h-screen\|100vh' src                         # неверная высота во Telegram
grep -rn 'style={{' src                                # инлайн-стили
grep -rnE 'className="[^"]{120,}"' src                 # раздутые className
```

## Приёмы

### Хардкод цвета → токен

```tsx
- <div className="bg-white text-gray-900 border-gray-200">
+ <div className="bg-card text-card-foreground border-border">
```

Нет подходящего токена — добавь его в `src/app/styles/index.css` (`:root`, `.dark`,
`@theme inline`), не подбирай близкий по смыслу.

### Тернарники → cva

Было:

```tsx
className={cn(
  'rounded-lg px-4',
  variant === 'primary' && 'bg-primary text-primary-foreground',
  variant === 'ghost' && 'hover:bg-accent',
  size === 'sm' ? 'h-9 text-sm' : 'h-11',
)}
```

Стало:

```tsx
const cardVariants = cva('rounded-lg px-4', {
  variants: {
    variant: {
      primary: 'bg-primary text-primary-foreground',
      ghost: 'hover:bg-accent',
    },
    size: { sm: 'h-9 text-sm', md: 'h-11' },
  },
  defaultVariants: { variant: 'primary', size: 'md' },
})
```

Порог: три и более варианта или два независимых измерения (variant + size).

### Повтор набора классов → компонент

Один и тот же набор встретился **третий** раз — вынеси компонент:

- набор без домена → `shared/ui/<Имя>.tsx`;
- набор с доменной семантикой → `domains/<домен>/ui/<Имя>.tsx`.

Два раза — оставь как есть. Преждевременная абстракция хуже дубля.

### Длинный className → структура

Больше ~10 классов в одной строке обычно значит, что элемент делает слишком много.
Сначала попробуй разбить разметку на подкомпоненты, и только потом — переносить классы
в переменную.

### Инлайн-стиль

Допустим только для вычисляемых на рантайме значений (позиция при драге, прогресс).
В таком случае оставь комментарий, почему нельзя классом.

## Чего не делать

- Не вводи собственные CSS-классы и `@apply`-компоненты в `index.css`.
- Не меняй разметку и семантику под предлогом чистки стилей — это отдельный коммит.
- Не гоняй `--fix` по всему проекту одним заходом: мелкими проходами, каждый проверяемый.
