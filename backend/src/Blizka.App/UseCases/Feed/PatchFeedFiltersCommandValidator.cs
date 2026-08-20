using FluentValidation;

namespace Blizka.App.UseCases.Feed;

public sealed class PatchFeedFiltersCommandValidator : AbstractValidator<PatchFeedFiltersCommand>
{
    public PatchFeedFiltersCommandValidator()
    {
        RuleFor(x => x.ShowGender!.Value).IsInEnum().When(x => x.ShowGender is not null);

        When(x => x.AgeRange is not null, () =>
        {
            RuleFor(x => x.AgeRange!.Min).InclusiveBetween(18, 99);
            RuleFor(x => x.AgeRange!.Max).InclusiveBetween(18, 99);
            RuleFor(x => x.AgeRange!)
                .Must(range => range.Min < range.Max)
                .WithMessage("AgeRange.Min must be less than AgeRange.Max.");
        });

        RuleFor(x => x.MaxDistanceKm!.Value).GreaterThan(0).When(x => x.MaxDistanceKm is not null);

        // Положительное число либо ровно ClearActiveWithinDays (-1, сентинел "выключить фильтр") — не любое
        // отрицательное значение, чтобы опечатка вроде -5 не читалась молча как "выключить".
        RuleFor(x => x.ActiveWithinDays!.Value)
            .Must(value => value > 0 || value == PatchFeedFiltersCommand.ClearActiveWithinDays)
            .WithMessage($"ActiveWithinDays must be a positive number of days, or {PatchFeedFiltersCommand.ClearActiveWithinDays} to clear the filter.")
            .When(x => x.ActiveWithinDays is not null);

        RuleForEach(x => x.DatingGoals).IsInEnum().When(x => x.DatingGoals is not null);
    }
}
