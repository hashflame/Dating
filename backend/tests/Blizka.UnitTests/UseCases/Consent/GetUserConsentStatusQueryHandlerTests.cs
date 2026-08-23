using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Consent;

namespace Blizka.UnitTests.UseCases.Consent;

public sealed class GetUserConsentStatusQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА согласий ещё не было ТОГДА все типы возвращаются с Given=false")]
    public async Task Handle_returns_given_false_when_no_consents_exist()
    {
        var handler = new GetUserConsentStatusQueryHandler(new FakeUserConsentRepository());

        var result = await handler.Handle(new GetUserConsentStatusQuery(Guid.NewGuid()), CancellationToken.None);

        var status = Assert.Single(result);
        Assert.Equal(ConsentType.TermsAndPrivacyPolicy, status.Type);
        Assert.False(status.Given);
        Assert.Null(status.Version);
        Assert.Null(status.Timestamp);
    }

    [Fact(DisplayName = "КОГДА есть несколько согласий одного типа ТОГДА возвращается самое свежее по Timestamp")]
    public async Task Handle_returns_the_latest_consent_by_timestamp()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeUserConsentRepository();
        repository.Consents.Add(new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = ConsentType.TermsAndPrivacyPolicy,
            Version = "1.0",
            Timestamp = DateTimeOffset.UtcNow.AddDays(-10),
        });
        repository.Consents.Add(new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = ConsentType.TermsAndPrivacyPolicy,
            Version = "2.0",
            Timestamp = DateTimeOffset.UtcNow,
        });
        var handler = new GetUserConsentStatusQueryHandler(repository);

        var result = await handler.Handle(new GetUserConsentStatusQuery(userId), CancellationToken.None);

        var status = Assert.Single(result);
        Assert.True(status.Given);
        Assert.Equal("2.0", status.Version);
    }

    private sealed class FakeUserConsentRepository : IUserConsentRepository
    {
        public List<UserConsent> Consents { get; } = [];

        public Task AddAsync(UserConsent consent, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Добавление согласия не используется в тестах чтения статуса.");

        public Task<bool> HasConsentAsync(Guid userId, ConsentType type, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Проверка HasConsentAsync не используется в тестах чтения статуса.");

        public Task<List<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Consents.Where(c => c.UserId == userId).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Сохранение не используется в тестах чтения статуса.");
    }
}
