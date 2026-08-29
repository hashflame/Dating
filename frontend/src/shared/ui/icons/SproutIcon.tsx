import { Icon, type IconProps } from './Icon'

/** Росток — отношения без спешки. */
export function SproutIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M12.3 22 V9.8" />
      <path d="M12.3 13.6 C7.4 13.6 3.6 10.1 3.6 5.2 c4.9 0 8.7 3.5 8.7 8.4z" />
      <path d="M12.3 10.4 c0 -4.8 3.5 -8.4 8.1 -8.4 c0 4.8 -3.5 8.4 -8.1 8.4z" />
    </Icon>
  )
}
