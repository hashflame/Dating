import { Component, type ReactNode } from 'react'

import { ErrorState } from '@/shared/ui'

type ErrorBoundaryProps = {
  children: ReactNode
}

type ErrorBoundaryState = {
  error: Error | null
}

/**
 * Без этого перехватчика необработанная ошибка рендера рвёт всё дерево, экран остаётся
 * пустым, а на реальных Telegram-клиентах devtools недоступны — ошибку не увидеть.
 * React не даёт перехватывать такие ошибки через хуки, только через классовый компонент.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  override state: ErrorBoundaryState = { error: null }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error }
  }

  override render() {
    const { error } = this.state
    if (error) {
      return <ErrorState description={`${error.name}: ${error.message}`} />
    }

    return this.props.children
  }
}
