import { Icon, type IconProps } from './Icon'

/** Карточка с портретом и строками — заполненная анкета. */
export function ProfileCardIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <rect x="2" y="3.5" width="20" height="17" rx="3" />
      <circle cx="8.8" cy="10" r="2.6" />
      <path d="M5 17c.6-2 2-3 3.8-3s3.2 1 3.8 3" />
      <path d="M15.3 9h4" />
      <path d="M15.3 12.8h4" />
    </Icon>
  )
}
