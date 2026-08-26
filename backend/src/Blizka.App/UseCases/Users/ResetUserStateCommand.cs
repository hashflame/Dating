using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>
/// <c>POST /api/dev/reset-my-state</c> (dev-инструмент, тикет ClickUp 869epwyw2) — приводит текущего
/// аутентифицированного пользователя в состояние "как сразу после онбординга", чтобы тестовый аккаунт
/// можно было заново прогонять через ленту/мэтчи/зорки без создания нового Telegram-аккаунта на каждый
/// прогон. В отличие от <see cref="Onboarding.DeleteOnboardingDraftCommand"/> (сбрасывает Status обратно
/// в New для повторного прохода самого онбординга), этот эндпоинт оставляет пользователя <c>Active</c> —
/// онбординг проходить заново не нужно, сбрасывается всё, что накопилось после него.
/// </summary>
public sealed record ResetUserStateCommand(Guid UserId) : IRequest;
