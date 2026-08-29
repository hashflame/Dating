import { Icon, type IconProps } from './Icon'

/** Перечёркнутый бокал — не пьёт. */
export function NoAlcoholIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M6 2 h11.9 l-0.9 6.2 a5 5 0 0 1 -10 0z" />
      <path d="M12 13.3 v7.8" />
      <path d="M7.4 22 h9.2" />
      <path d="M3.9 20.1 20.1 3.9" />
    </Icon>
  )
}
