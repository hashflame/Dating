import { Icon, type IconProps } from './Icon'

/** Облако реплики с многоточием — общение и переписка. */
export function ChatBubbleIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M2 6a3 3 0 0 1 3-3h14a3 3 0 0 1 3 3v6.5a3 3 0 0 1-3 3H9.5L2 21.5z" />
      <circle cx="8" cy="9.2" r="1.2" fill="currentColor" stroke="none" />
      <circle cx="12" cy="9.2" r="1.2" fill="currentColor" stroke="none" />
      <circle cx="16" cy="9.2" r="1.2" fill="currentColor" stroke="none" />
    </Icon>
  )
}
