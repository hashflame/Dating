namespace Blizka.App.Domain.Enums;

public enum InterestCategory
{
    Sport,
    Creativity,
    Entertainment,
    FoodAndDrinks,
    GrowthAndTravel,

    /// <summary>
    /// Категория для интересов, созданных пользователями (T-9.2, <c>Interest.IsCustom</c>) — decomposition.md
    /// не описывает, к какой категории они относятся, а каталог группируется по категориям, поэтому им нужна своя.
    /// </summary>
    Custom,
}
