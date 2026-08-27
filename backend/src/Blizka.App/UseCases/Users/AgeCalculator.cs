namespace Blizka.App.UseCases.Users;

/// <summary>
/// Общий расчёт возраста из даты рождения — раньше был продублирован по отдельности в
/// <c>UserProfileMapper</c>/<c>GetUserProfileQueryHandler</c>/<c>GetProfilePreviewQueryHandler</c> (баг из
/// e2e-прогона: до завершения шага 1 онбординга в БД лежит <c>BirthDate = DateOnly.MinValue</c>, и расчёт
/// возраста от неё давал бессмысленное число вроде 2025).
/// </summary>
internal static class AgeCalculator
{
    /// <returns><c>null</c>, если дата рождения ещё не задана (<paramref name="birthDate"/> — дефолтное значение).</returns>
    public static int? Calculate(DateOnly birthDate)
    {
        if (birthDate == default)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
