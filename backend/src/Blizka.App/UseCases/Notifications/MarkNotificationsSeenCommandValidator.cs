using FluentValidation;

namespace Blizka.App.UseCases.Notifications;

public sealed class MarkNotificationsSeenCommandValidator : AbstractValidator<MarkNotificationsSeenCommand>
{
    public MarkNotificationsSeenCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Likes || x.Matches)
            .WithName("likes")
            .WithMessage("Нужно выставить хотя бы один из флагов: likes или matches.");
    }
}
