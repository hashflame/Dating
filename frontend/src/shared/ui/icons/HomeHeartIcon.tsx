import { Icon, type IconProps } from './Icon'

/** Дом с сердцем — семья и дети. */
export function HomeHeartIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M2 10.3 L12 2.5 l10 7.8 V19.1 a2.4 2.4 0 0 1 -2.4 2.4 H4.4 a2.4 2.4 0 0 1 -2.4 -2.4z" />
      <path d="M12 18.2 c-2.3 -1.6 -3.7 -2.9 -3.7 -4.3 a1.9 1.9 0 0 1 3.7 -1 a1.9 1.9 0 0 1 3.7 1 c0 1.3 -1.3 2.7 -3.7 4.3z" />
    </Icon>
  )
}
