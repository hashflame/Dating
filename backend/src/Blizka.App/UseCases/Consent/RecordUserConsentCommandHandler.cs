using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Consent;

/// <summary>
/// Фиксирует юридическое согласие пользователя (T-2.2) как новую запись в append-only логе —
/// повторное согласие (например, с новой версией документа) не перезаписывает предыдущее.
/// </summary>
public sealed class RecordUserConsentCommandHandler(
    IUserConsentRepository consentRepository,
    IValidator<RecordUserConsentCommand> validator)
    : IRequestHandler<RecordUserConsentCommand, UserConsentResult>
{
    public async Task<UserConsentResult> Handle(RecordUserConsentCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var consent = new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TelegramId = request.TelegramId,
            Type = request.Type,
            Version = request.Version,
            AgeConfirmed = request.AgeConfirmed,
            IpAddress = request.IpAddress,
            Timestamp = DateTimeOffset.UtcNow,
        };

        await consentRepository.AddAsync(consent, cancellationToken);
        await consentRepository.SaveChangesAsync(cancellationToken);

        return new UserConsentResult(consent.Type, consent.Version, consent.Timestamp);
    }
}
