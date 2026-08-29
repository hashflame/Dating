export {
  getPlatform,
  getRawInitData,
  getStartParam,
  openExternalLink,
  openTelegramChat,
  shareToTelegram,
} from './bridge'
export { getTelegramUser, initTelegram, OutsideTelegramError } from './init'
export { getDevUser, setDevUserId, type DevUser } from './dev-user'
export { getMockColorScheme, setMockColorScheme, type MockColorScheme } from './theme-mock'
export { useBackButton } from './use-back-button'
export { useHaptic } from './use-haptic'
