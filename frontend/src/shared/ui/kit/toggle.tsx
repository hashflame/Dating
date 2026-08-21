import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Toggle as TogglePrimitive } from "radix-ui"

import { cn } from "@/shared/lib/cn"

// Настроено под проект: вариант `surface` — выбираемая поверхность
// (метка, карточка варианта). Выбранное состояние фирменное, наведение нейтральное.
const toggleVariants = cva(
  "inline-flex items-center justify-center gap-2 rounded-md text-sm font-medium outline-none transition-[color,background-color,border-color,box-shadow,transform] duration-150 focus-visible:ring-[3px] focus-visible:ring-ring/40 disabled:pointer-events-none disabled:opacity-40 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default:
          "whitespace-nowrap bg-transparent hover:bg-accent data-[state=on]:bg-accent data-[state=on]:text-accent-foreground",
        outline:
          "whitespace-nowrap border border-input bg-transparent hover:bg-accent data-[state=on]:bg-accent",
        // Сегмент внутри дорожки: выбранный поднимается «пилюлей».
        segment:
          'whitespace-nowrap text-muted-foreground hover:text-foreground data-[state=on]:bg-card data-[state=on]:text-brand data-[state=on]:shadow-sm data-[state=on]:font-semibold',
        surface:
          "border border-border bg-card text-foreground hover:border-brand/35 hover:bg-brand-soft/50 active:scale-[0.99] data-[state=on]:border-brand data-[state=on]:bg-brand-soft data-[state=on]:text-brand",
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
