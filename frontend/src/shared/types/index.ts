/** Значение, которое может отсутствовать в ответе API. */
export type Nullable<T> = T | null

/** Делает перечисленные поля обязательными. */
export type RequireKeys<T, K extends keyof T> = T & { [P in K]-?: T[P] }
