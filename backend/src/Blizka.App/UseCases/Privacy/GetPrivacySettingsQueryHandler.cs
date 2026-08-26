using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Privacy;

/// <summary>
/// Обрабатывает <see cref="GetPrivacySettingsQuery"/> (T-16.1). Отсутствие строки в БД — не 404, а значения
/// по умолчанию (<see cref="PrivacySettingsDefaults"/>): пользователь ещё не открывал экран настроек приватности.
/// </summary>
public sealed class GetPrivacySettingsQueryHandler(IPrivacySettingsRepository repository)
    : IRequestHandler<GetPrivacySettingsQuery, PrivacySettingsResult>
{
    public async Task<PrivacySettingsResult> Handle(GetPrivacySettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await repository.GetByUserIdAsync(request.UserId, cancellationToken);
        return PrivacySettingsDefaults.ToResult(settings);
    }
}
