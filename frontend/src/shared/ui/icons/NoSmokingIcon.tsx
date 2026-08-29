import { Icon, type IconProps } from './Icon'

/** Перечёркнутая сигарета — не курит. */
export function NoSmokingIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <rect x="2" y="13.2" width="14.8" height="4" rx="2" />
      <path d="M12.8 13.2 v4" />
      <path d="M20.1 11.8 c1.9 -1.4 1.9 -3.5 0 -4.9" />
      <path d="M3.9 20.1 20.1 3.9" />
    </Icon>
  )
}
