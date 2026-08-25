namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда среди переданных <c>interestIds</c> есть id, отсутствующий в каталоге (T-9.2).</summary>
public sealed class InterestNotFoundException(Guid interestId)
    : BlizkaDomainException(
        "INTEREST_NOT_FOUND",
        $"Interest {interestId} was not found.",
        new Dictionary<string, object?> { ["interestId"] = interestId })
{
    public Guid InterestId { get; } = interestId;
}
