using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Users;
using NetTopologySuite.Geometries;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class GetProfilePreviewQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА профиль заполнен ТОГДА превью содержит возраст, город, фото и интересы на локали запроса")]
    public async Task Handle_returns_the_profile_preview()
    {
        var city = new City
        {
            Id = Guid.NewGuid(),
            NameRu = "Минск",
            NameBe = "Мінск",
            NameEn = "Minsk",
            Coordinates = new Point(27.56, 53.9) { SRID = 4326 },
        };
        var interest = new Interest { Id = Guid.NewGuid(), NameRu = "Скалолазание", NameBe = "Скалалажанне", NameEn = "Climbing" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Ann",
            Bio = "Hello",
            BirthDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date.AddYears(-25)),
            City = city,
            CityId = city.Id,
            IsVerified = true,
            DatingGoal = DatingGoal.Casual,
            Prompts = ["Favorite trip?"],
        };
        user.Photos.Add(new Photo
        {
            Id = Guid.NewGuid(), UserId = user.Id, Url = "https://cdn/a.jpg",
            ThumbnailUrl = "https://cdn/a-thumb.jpg", MediumUrl = "https://cdn/a-medium.jpg", IsMain = true,
        });
        user.UserInterests.Add(new UserInterest { UserId = user.Id, InterestId = interest.Id, Interest = interest });
        var handler = new GetProfilePreviewQueryHandler(new FakeUserRepository(user), new FakePrivacySettingsRepository());

        var result = await handler.Handle(new GetProfilePreviewQuery(user.Id, "en"), CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("Ann", result.Name);
        Assert.Equal(25, result.Age);
        Assert.Equal("Hello", result.Bio);
        Assert.Equal("Minsk", result.CityName);
        Assert.True(result.IsVerified);
        Assert.Equal(DatingGoal.Casual, result.DatingGoal);
        Assert.Equal(["Favorite trip?"], result.Prompts);
        var photo = Assert.Single(result.Photos);
        Assert.True(photo.IsMain);
        var resolvedInterest = Assert.Single(result.Interests);
        Assert.Equal("Climbing", resolvedInterest.Name);
    }

    [Fact(DisplayName = "КОГДА аутентифицированный пользователь не найден ТОГДА выбрасывается InvalidOperationException")]
    public async Task Handle_throws_when_the_user_is_missing()
    {
        var handler = new GetProfilePreviewQueryHandler(new FakeUserRepository(user: null), new FakePrivacySettingsRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetProfilePreviewQuery(Guid.NewGuid(), "ru"), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА пользователь включил hideAge ТОГДА возраст в превью null")]
    public async Task Handle_hides_the_age_when_hide_age_is_enabled()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Ann",
            BirthDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date.AddYears(-25)),
        };
        var privacyRepository = new FakePrivacySettingsRepository();
        privacyRepository.ByUserId[user.Id] = new PrivacySettings { UserId = user.Id, HideAge = true };
        var handler = new GetProfilePreviewQueryHandler(new FakeUserRepository(user), privacyRepository);

        var result = await handler.Handle(new GetProfilePreviewQuery(user.Id, "ru"), CancellationToken.None);

        Assert.Null(result.Age);
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetProfilePreviewQueryHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(user?.Id == id ? user : null);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetProfilePreviewQueryHandler.");

        public Task AddAsync(User newUser, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetProfilePreviewQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetProfilePreviewQueryHandler.");
    }

    private sealed class FakePrivacySettingsRepository : IPrivacySettingsRepository
    {
        public Dictionary<Guid, PrivacySettings> ByUserId { get; } = [];

        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ByUserId.GetValueOrDefault(userId));

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetProfilePreviewQueryHandler.");

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetProfilePreviewQueryHandler.");

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetProfilePreviewQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetProfilePreviewQueryHandler.");
    }
}
