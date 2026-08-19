let token: string | null = null

/**
 * Сессионный JWT держим только в памяти: он короткоживущий и в любой момент
 * переполучается обменом initData, так что localStorage — риск без выигрыша.
 * Пишет домен `session`, читает `apiRequest`.
 */
export function setAuthToken(value: string | null): void {
  token = value
}

export function getAuthToken(): string | null {
  return token
}
