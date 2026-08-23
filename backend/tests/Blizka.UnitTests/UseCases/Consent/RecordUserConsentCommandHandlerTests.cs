using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Consent;
using FluentValidation;

namespace Blizka.UnitTests.UseCases.Consent;

public sealed class RecordUserConsentCommandHandlerTests
{
    private static RecordUserConsentCommandHandler CreateHandler(FakeUserConsentRepository repository) =>
        new(repository, new RecordUserConsentCommandValidator());

    [Fact(DisplayName = "КОГДА пользователь фиксирует согласие ТОГДА в репозиторий добавляется новая запись с его данными")]
    public async Task Handle_adds_a_new_consent_record()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeUserConsentRepository();
        var handler = CreateHandler(repository);
        var command = new RecordUserConsentCommand(userId, 123456, ConsentType.TermsAndPrivacyPolicy, "1.0", true, "203.0.113.5");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ConsentType.TermsAndPrivacyPolicy, result.Type);
        Assert.Equal("1.0", result.Version);
        var stored = Assert.Single(repository.Consents);
        Assert.Equal(userId, stored.UserId);
        Assert.Equal(123456, stored.TelegramId);
        Assert.Equal("203.0.113.5", stored.IpAddress);
    }

    [Fact(DisplayName = "КОГДА пользователь повторно фиксирует согласие с новой версией ТОГДА обе записи сохраняются в истории")]
    public async Task Handle_keeps_history_across_repeated_consents()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeUserConsentRepository();
        var handler = CreateHandler(repository);

        await handler.Handle(new RecordUserConsentCommand(userId, 1, ConsentType.TermsAndPrivacyPolicy, "1.0", true, null), CancellationToken.None);
        await handler.Handle(new RecordUserConsentCommand(userId, 1, ConsentType.TermsAndPrivacyPolicy, "2.0", true, null), CancellationToken.None);

        Assert.Equal(2, repository.Consents.Count);
        Assert.True(await repository.HasConsentAsync(userId, ConsentType.TermsAndPrivacyPolicy, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА версия документа не указана ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_ValidationException_for_an_empty_version()
    {
        var repository = new FakeUserConsentRepository();
        var handler = CreateHandler(repository);
        var command = new RecordUserConsentCommand(Guid.NewGuid(), 1, ConsentType.TermsAndPrivacyPolicy, string.Empty, true, null);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА версия документа длиннее 32 символов (лимит колонки UserConsent.Version) ТОГДА выбрасывается ValidationException, а не падает на SaveChangesAsync")]
    public async Task Handle_throws_ValidationException_for_a_version_longer_than_the_column_limit()
    {
        var repository = new FakeUserConsentRepository();
        var handler = CreateHandler(repository);
        var tooLongVersion = new string('1', 33);
        var command = new RecordUserConsentCommand(Guid.NewGuid(), 1, ConsentType.TermsAndPrivacyPolicy, tooLongVersion, true, null);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Empty(repository.Consents);
    }

    [Fact(DisplayName = "КОГДА AgeConfirmed не передан для TermsAndPrivacyPolicy ТОГДА выбрасывается ValidationException (spec 002, B4)")]
    public async Task Handle_throws_ValidationException_when_age_is_not_confirmed()
    {
        var repository = new FakeUserConsentRepository();
        var handler = CreateHandler(repository);
        var command = new RecordUserConsentCommand(Guid.NewGuid(), 1, ConsentType.TermsAndPrivacyPolicy, "1.0", false, null);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Empty(repository.Consents);
    }

    private sealed class FakeUserConsentRepository : IUserConsentRepository
    {
        public List<UserConsent> Consents { get; } = [];

        public Task AddAsync(UserConsent consent, CancellationToken cancellationToken)
        {
            Consents.Add(consent);
            return Task.CompletedTask;
        }

        public Task<bool> HasConsentAsync(Guid userId, ConsentType type, CancellationToken cancellationToken) =>
            Task.FromResult(Consents.Any(c => c.UserId == userId && c.Type == type));

        public Task<List<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Consents.Where(c => c.UserId == userId).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
