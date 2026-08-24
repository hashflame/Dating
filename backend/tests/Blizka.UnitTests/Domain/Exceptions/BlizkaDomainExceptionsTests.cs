using Blizka.App.Domain.Exceptions;

namespace Blizka.UnitTests.Domain.Exceptions;

public sealed class BlizkaDomainExceptionsTests
{
    [Fact(DisplayName = "КОГДА создано InsufficientSparksException ТОГДА оно содержит код ошибки и суммы в Details")]
    public void InsufficientSparksException_carries_error_code_and_amounts_in_details()
    {
        var exception = new InsufficientSparksException(required: 10, available: 3);

        Assert.Equal("INSUFFICIENT_SPARKS", exception.ErrorCode);
        Assert.Equal(10, exception.Required);
        Assert.Equal(3, exception.Available);
        Assert.Equal(10, exception.Details!["required"]);
        Assert.Equal(3, exception.Details!["available"]);
    }

    [Fact(DisplayName = "КОГДА создано UserBannedException ТОГДА оно содержит код ошибки и reason/expiresAt в Details (spec 002, B2)")]
    public void UserBannedException_carries_error_code_and_reason_expiresAt_in_details()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var exception = new UserBannedException(userId, "spam", expiresAt);

        Assert.Equal("USER_BANNED", exception.ErrorCode);
        Assert.Equal(userId, exception.UserId);
        Assert.Equal("spam", exception.Details!["reason"]);
        Assert.Equal(expiresAt, exception.Details!["expiresAt"]);
    }

    [Fact(DisplayName = "КОГДА создано UserBannedException без причины и срока ТОГДА Details содержит null-значения, а не ошибку")]
    public void UserBannedException_allows_null_reason_and_expiresAt()
    {
        var userId = Guid.NewGuid();

        var exception = new UserBannedException(userId, null, null);

        Assert.Null(exception.Details!["reason"]);
        Assert.Null(exception.Details!["expiresAt"]);
    }

    [Fact(DisplayName = "КОГДА создано UserDeletedException ТОГДА оно содержит код ошибки и userId в Details")]
    public void UserDeletedException_carries_error_code_and_userId_in_details()
    {
        var userId = Guid.NewGuid();

        var exception = new UserDeletedException(userId);

        Assert.Equal("USER_DELETED", exception.ErrorCode);
        Assert.Equal(userId, exception.UserId);
        Assert.Equal(userId, exception.Details!["userId"]);
    }

    [Fact(DisplayName = "КОГДА OnboardingIncompleteException создано без шага ТОГДА Details отсутствуют")]
    public void OnboardingIncompleteException_without_step_has_no_details()
    {
        var exception = new OnboardingIncompleteException();

        Assert.Equal("ONBOARDING_INCOMPLETE", exception.ErrorCode);
        Assert.Null(exception.MissingStep);
        Assert.Null(exception.Details);
    }

    [Fact(DisplayName = "КОГДА OnboardingIncompleteException создано с шагом ТОГДА шаг попадает в Details")]
    public void OnboardingIncompleteException_with_step_reports_it_in_details()
    {
        var exception = new OnboardingIncompleteException("photos");

        Assert.Equal("photos", exception.MissingStep);
        Assert.Equal("photos", exception.Details!["missingStep"]);
    }

    [Fact(DisplayName = "КОГДА создано CityNotOpenException ТОГДА оно содержит код ошибки и cityId в Details")]
    public void CityNotOpenException_carries_error_code_and_cityId_in_details()
    {
        var cityId = Guid.NewGuid();

        var exception = new CityNotOpenException(cityId);

        Assert.Equal("CITY_NOT_OPEN", exception.ErrorCode);
        Assert.Equal(cityId, exception.CityId);
        Assert.Equal(cityId, exception.Details!["cityId"]);
    }

    [Fact(DisplayName = "КОГДА создано TelegramAvatarDownloadFailedException ТОГДА оно содержит код ошибки, photoUrl в Details и внутреннее исключение")]
    public void TelegramAvatarDownloadFailedException_carries_error_code_and_photoUrl_in_details()
    {
        var photoUrl = new Uri("https://t.me/i/userpic/320/dev_user.jpg");
        var inner = new HttpRequestException("404");

        var exception = new TelegramAvatarDownloadFailedException(photoUrl, inner);

        Assert.Equal("PHOTO_DOWNLOAD_FAILED", exception.ErrorCode);
        Assert.Equal(photoUrl, exception.PhotoUrl);
        Assert.Equal(photoUrl.ToString(), exception.Details!["photoUrl"]);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact(DisplayName = "КОГДА создано OnboardingDraftResetConflictException ТОГДА оно содержит код ошибки, userId в Details и внутреннее исключение")]
    public void OnboardingDraftResetConflictException_carries_error_code_and_userId_in_details()
    {
        var userId = Guid.NewGuid();
        var inner = new InvalidOperationException("simulated xmin conflict");

        var exception = new OnboardingDraftResetConflictException(userId, inner);

        Assert.Equal("ONBOARDING_DRAFT_RESET_CONFLICT", exception.ErrorCode);
        Assert.Equal(userId, exception.UserId);
        Assert.Equal(userId, exception.Details!["userId"]);
        Assert.Same(inner, exception.InnerException);
    }
}
