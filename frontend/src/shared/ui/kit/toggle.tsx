import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Toggle as TogglePrimitive } from "radix-ui"

import { cn } from "@/shared/lib/cn"

// Настроено под проект: вариант `surface` — выбираемая поверхность
// (метка, карточка варианта), `segment` — сегмент внутри дорожки.
//
// Выбранное состояние — сплошная фирменная заливка, а не подкрашенная рамка:
// в макетах рамок нет вообще, и «выбрано» несёт цвет, а не контур.
const toggleVariants = cva(
  "inline-flex items-center justify-center gap-2 rounded-md text-sm font-medium outline-none transition-[color,background-color,border-color,box-shadow,transform] duration-150 focus-visible:ring-[3px] focus-visible:ring-ring/40 disabled:pointer-events-none disabled:opacity-40 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default:
          "whitespace-nowrap bg-transparent hover:bg-accent data-[state=on]:bg-accent data-[state=on]:text-accent-foreground",
        outline:
          "whitespace-nowrap bg-surface hover:bg-surface-strong data-[state=on]:bg-brand data-[state=on]:text-brand-foreground",
        // Сегмент внутри дорожки: выбранный заливается фирменным и получает
        // мягкий ореол — тот же приём, что у круглых кнопок ленты.
        segment:
          'whitespace-nowrap text-muted-foreground hover:text-foreground data-[state=on]:bg-brand data-[state=on]:text-brand-foreground data-[state=on]:shadow-glow-brand data-[state=on]:font-semibold',
        surface:
          "bg-surface text-foreground hover:bg-surface-strong active:scale-[0.99] data-[state=on]:bg-brand data-[state=on]:text-brand-foreground data-[state=on]:shadow-glow-brand",
      },
      size: {
        default: "h-9 min-w-9 px-2",
        sm: "h-8 min-w-8 px-1.5",
        lg: "h-10 min-w-10 px-2.5",
        none: "",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Toggle({
  className,
  variant,
  size,
  ...props
}: React.ComponentProps<typeof TogglePrimitive.Root> &
  VariantProps<typeof toggleVariants>) {
  return (
    <TogglePrimitive.Root
      data-slot="toggle"
      className={cn(toggleVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Toggle, toggleVariants }
