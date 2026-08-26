using FluentValidation;

namespace Blizka.App.UseCases.Blocks;

public sealed class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
{
    public BlockUserCommandValidator()
    {
        RuleFor(x => x.BlockedUserId)
            .NotEqual(x => x.BlockerUserId)
            .WithMessage("Нельзя заблокировать самого себя.");
    }
}
