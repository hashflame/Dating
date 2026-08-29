import { Icon, type IconProps } from './Icon'

/** Фотоаппарат — требование фото в анкете. */
export function CameraIcon({ className }: IconProps) {
  return (
    <Icon className={className}>
      <path d="M2 9a3 3 0 0 1 3-3h1.6l1.5-2.5h7.8L17.4 6H19a3 3 0 0 1 3 3v8a3 3 0 0 1-3 3H5a3 3 0 0 1-3-3z" />
      <circle cx="12" cy="13" r="3.8" />
    </Icon>
  )
}
