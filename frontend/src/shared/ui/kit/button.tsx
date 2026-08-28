import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'
import * as React from 'react'

import { cn } from '@/shared/lib/cn'

// Настроено под проект: кнопки — «пилюли» без рамок. Форму держит скругление
// и заливка, а главное действие дополнительно подсвечено ореолом — в макетах
// это единственный источник цвета на плоском фоне.
const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-full text-sm font-semibold transition-all disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg:not([class*='size-'])]:size-4 shrink-0 [&_svg]:shrink-0 outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40 active:scale-[0.98]",
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground shadow-glow-brand hover:bg-primary/90',
        secondary: 'bg-surface-strong text-foreground hover:bg-surface-strong/70',
        // Раньше был контурным. Рамок в макетах нет, поэтому «второстепенная,
        // но заметная» кнопка теперь отличается от `secondary` тише — заливкой
        // на ступень слабее.
        outline: 'bg-surface text-foreground hover:bg-surface-strong',
        ghost: 'hover:bg-accent',
        link: 'text-link underline-offset-4 hover:underline',
        destructive:
          'bg-destructive text-destructive-foreground shadow-glow-brand hover:bg-destructive/90',
      },
      size: {
        default: 'h-11 px-5 has-[>svg]:px-4',
        sm: 'h-9 gap-1.5 px-4 has-[>svg]:px-3',
        // Главное действие экрана из макетов: заметно выше остальных.
        lg: 'h-14 px-6 text-base has-[>svg]:px-5',
        icon: 'size-11',
      },
      block: {
        true: 'w-full',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
)

type ButtonProps = React.ComponentProps<'button'> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }

function Button({ className, variant, size, block, asChild = false, ...props }: ButtonProps) {
  const Component = asChild ? Slot : 'button'

  return (
    <Component
      data-slot="button"
      className={cn(buttonVariants({ variant, size, block, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants, type ButtonProps }
