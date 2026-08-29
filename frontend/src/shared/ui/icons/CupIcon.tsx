import { Icon, type IconProps } from './Icon'

/** Чашка с паром — спокойные посиделки. */
export function CupIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M3.6 8.8 h13.2 v6.2 a6.6 6.6 0 0 1 -13.2 0z" />
      <path d="M16.8 10.3 h1.8 a3.1 3.1 0 0 1 0 6.2 h-1.8" />
      <path d="M2.3 22 h15.8" />
      <path d="M8 5.1 c0 -1.3 1.2 -1.7 1.2 -3.1" />
      <path d="M12.7 5.1 c0 -1.3 1.2 -1.7 1.2 -3.1" />
    </Icon>
  )
}
