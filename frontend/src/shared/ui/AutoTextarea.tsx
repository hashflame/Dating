import { useLayoutEffect, useRef, type ComponentProps } from 'react'

import { cn } from '@/shared/lib'

import { Textarea } from './kit/textarea'

type AutoTextareaProps = ComponentProps<typeof Textarea>

/**
 * Поле ввода, которое растёт под текст само.
 *
 * Уголок ручного ресайза убран: в мини-аппе его тянут случайно вместе со
 * скроллом, а растянутое поле остаётся таким навсегда. Высота вместо этого
 * всегда равна содержимому.
 *
 * `field-sizing-content` из примитива умеет это сам, но только в Chromium —
 * в Telegram на iOS (WebKit) он не работает, поэтому высоту пересчитываем
 * руками. Оба механизма не мешают друг другу: там, где работает нативный,
 * пересчёт даёт то же значение.
 */
export function AutoTextarea({ className, onChange, ...props }: AutoTextareaProps) {
  const ref = useRef<HTMLTextAreaElement>(null)

  const resize = (element: HTMLTextAreaElement | null): void => {
    if (element === null) return

    // Сброс перед замером обязателен: `scrollHeight` не уменьшается сам,
    // и после удаления строк поле осталось бы высоким.
    element.style.height = 'auto'
    element.style.height = `${element.scrollHeight}px`
  }

  // Не в onChange: высоту надо поправить и когда значение пришло снаружи —
  // например черновик подставили кнопкой заготовки.
  useLayoutEffect(() => resize(ref.current), [props.value])

  return (
    <Textarea
      ref={ref}
      className={cn('resize-none overflow-hidden', className)}
      onChange={(event) => {
        resize(event.currentTarget)
        onChange?.(event)
      }}
      {...props}
    />
  )
}
