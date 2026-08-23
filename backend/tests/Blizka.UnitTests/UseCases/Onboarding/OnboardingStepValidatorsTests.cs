using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Onboarding;
using NetTopologySuite.Geometries;

namespace Blizka.UnitTests.UseCases.Onboarding;

public sealed class OnboardingStepValidatorsTests
{
    [Fact(DisplayName = "КОГДА имя пустое ТОГДА шаг 1 не проходит валидацию")]
    public async Task Step1_fails_when_name_is_empty()
    {
        var validator = new OnboardingStep1DataValidator();
        var data = new OnboardingStep1Data(string.Empty, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)), Gender.Male);

        var result = await validator.ValidateAsync(data);

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "КОГДА возраст меньше 18 лет ТОГДА шаг 1 не проходит валидацию")]
    public async Task Step1_fails_when_user_is_under_18()
    {
        var validator = new OnboardingStep1DataValidator();
        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17));
        var data = new OnboardingStep1Data("Ann", birthDate, Gender.Female);

        var result = await validator.ValidateAsync(data);

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "КОГДА данные шага 1 корректны ТОГДА валидация проходит")]
    public async Task Step1_succeeds_for_valid_data()
    {
        var validator = new OnboardingStep1DataValidator();
        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18));
        var data = new OnboardingStep1Data("Ann", birthDate, Gender.Female);

        var result = await validator.ValidateAsync(data);

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "КОГДА datingGoals пуст ТОГДА шаг 2 не проходит валидацию")]
    public async Task Step2_fails_when_dating_goals_is_empty()
    {
        var validator = new OnboardingStep2DataValidator();
        var data = new OnboardingStep2Data(ShowGenderPreference.All, new OnboardingAgeRange(18, 30), []);

        var result = await validator.ValidateAsync(data);

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "КОГДА ageRange отсутствует ТОГДА шаг 2 не проходит валидацию")]
    public async Task Step2_fails_when_age_range_is_missing()
    {
        var validator = new OnboardingStep2DataValidator();
        // System.Text.Json не проверяет non-nullable аннотации в рантайме — при отсутствии
        // ageRange в теле запроса десериализация всё равно даст null, несмотря на тип свойства.
        var data = new OnboardingStep2Data(ShowGenderPreference.All, null!, [DatingGoal.Casual]);

        var result = await validator.ValidateAsync(data);

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "КОГДА ageRange.min не меньше ageRange.max ТОГДА шаг 2 не проходит валидацию")]
    public async Task Step2_fails_when_age_range_min_is_not_less_than_max()
    {
        var validator = new OnboardingStep2DataValidator();
        var data = new OnboardingStep2Data(ShowGenderPreference.All, new OnboardingAgeRange(30, 30), [DatingGoal.Casual]);

        var result = await validator.ValidateAsync(data);

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "КОГДА данные шага 2 корректны ТОГДА валидация проходит")]
    public async Task Step2_succeeds_for_valid_data()
    {
        var validator = new OnboardingStep2DataValidator();
        var data = new OnboardingStep2Data(ShowGenderPreference.Female, new OnboardingAgeRange(20, 35), [DatingGoal.LongTermRelationship]);

        var result = await validator.ValidateAsync(data);

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "КОГДА cityId не существует в БД ТОГДА шаг 3 не проходит валидацию")]
    public async Task Step3_fails_when_city_does_not_exist()
    {
        var validator = new OnboardingStep3DataValidator(new FakeCityRepository(exists: false));

        var result = await validator.ValidateAsync(new OnboardingStep3Data(Guid.NewGuid()));

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "КОГДА cityId существует в БД ТОГДА шаг 3 проходит валидацию")]
    public async Task Step3_succeeds_when_city_exists()
    {
        var validator = new OnboardingStep3DataValidator(new FakeCityRepository(exists: true));

        var result = await validator.ValidateAsync(new OnboardingStep3Data(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    private sealed class FakeCityRepository(bool exists) : ICityRepository
    {
        public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) => Task.FromResult(exists);

        public Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Получение города по id не используется в тестах онбординга.");

        public Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Поиск городов не используется в тестах онбординга.");

        public Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Определение города по координатам не используется в тестах онбординга.");
    }
}
