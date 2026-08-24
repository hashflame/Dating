using Blizka.App.Domain.Repositories;

namespace Blizka.App.UseCases.Likes;

/// <summary>Общая проекция <see cref="LikeEntry"/> → <see cref="LikeUserResult"/>, разделяемая между тремя use case'ами T-6.1.</summary>
internal static class LikeResultMapper
{
    public static LikeUserResult ToUserResult(LikeEntry entry)
    {
        var mainPhoto = entry.User.Photos.FirstOrDefault(p => p.IsMain)
            ?? entry.User.Photos.OrderBy(p => p.SortOrder).FirstOrDefault();

        return new LikeUserResult(entry.User.Id, entry.User.Name, CalculateAge(entry.User.BirthDate), mainPhoto?.Url);
    }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var age = today.Year - birthDate.Year;
        if (today < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
