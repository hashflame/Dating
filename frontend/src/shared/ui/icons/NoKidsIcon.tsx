import { Icon, type IconProps } from './Icon'

/** Перечёркнутая коляска — без детей. */
export function NoKidsIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M2 12.2 h19.9 a9.9 9.9 0 0 0 -19.9 0z" />
      <path d="M11.9 2.6 v9.6" />
      <path d="M6.1 12.2 L4.9 17.3" />
      <path d="M18.1 12.2 l1.2 5.1" />
      <circle cx="4.3" cy="19.5" r="2.2" />
      <circle cx="19.8" cy="19.5" r="2.2" />
      <path d="M3.9 20.1 20.1 3.9" />
    </Icon>
  )
}
