using Blizka.App.Domain.Exceptions;

namespace Blizka.UnitTests.Domain.Exceptions;

public sealed class BlizkaDomainExceptionsTests
{
    [Fact]
    public void InsufficientSparksException_carries_error_code_and_amounts_in_details()
    {
        var exception = new InsufficientSparksException(required: 10, available: 3);

        Assert.Equal("INSUFFICIENT_SPARKS", exception.ErrorCode);
        Assert.Equal(10, exception.Required);
        Assert.Equal(3, exception.Available);
        Assert.Equal(10, exception.Details!["required"]);
        Assert.Equal(3, exception.Details!["available"]);
    }

    [Fact]
    public void UserBannedException_carries_error_code_and_userId_in_details()
    {
        var userId = Guid.NewGuid();

        var exception = new UserBannedException(userId);

        Assert.Equal("USER_BANNED", exception.ErrorCode);
        Assert.Equal(userId, exception.UserId);
        Assert.Equal(userId, exception.Details!["userId"]);
    }

    [Fact]
    public void OnboardingIncompleteException_without_step_has_no_details()
    {
        var exception = new OnboardingIncompleteException();

        Assert.Equal("ONBOARDING_INCOMPLETE", exception.ErrorCode);
        Assert.Null(exception.MissingStep);
        Assert.Null(exception.Details);
    }

    [Fact]
    public void OnboardingIncompleteException_with_step_reports_it_in_details()
    {
        var exception = new OnboardingIncompleteException("photos");

        Assert.Equal("photos", exception.MissingStep);
        Assert.Equal("photos", exception.Details!["missingStep"]);
    }

    [Fact]
    public void CityNotOpenException_carries_error_code_and_cityId_in_details()
    {
        var cityId = Guid.NewGuid();

        var exception = new CityNotOpenException(cityId);

        Assert.Equal("CITY_NOT_OPEN", exception.ErrorCode);
        Assert.Equal(cityId, exception.CityId);
        Assert.Equal(cityId, exception.Details!["cityId"]);
    }
}
