import { Icon, type IconProps } from './Icon'

/** Печать с галочкой — подтверждённый профиль. */
export function VerifiedIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M12 2 L14.2 3.6 L17 3.3 L18.2 5.8 L20.7 7 L20.4 9.8 L22 12 L20.4 14.2 L20.7 17 L18.2 18.2 L17 20.7 L14.2 20.4 L12 22 L9.8 20.4 L7 20.7 L5.8 18.2 L3.3 17 L3.6 14.2 L2 12 L3.6 9.8 L3.3 7 L5.8 5.8 L7 3.3 L9.8 3.6z" />
      <path d="M8.2 12.1 L10.8 14.7 L15.8 9.4" />
    </Icon>
  )
}
