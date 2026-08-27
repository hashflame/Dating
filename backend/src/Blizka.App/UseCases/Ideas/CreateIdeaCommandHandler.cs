using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Ideas;

/// <summary>
/// Обрабатывает <see cref="CreateIdeaCommand"/> (T-19.1): создаёт идею и начисляет <c>Sparks:IdeaSubmissionBonusAmount</c>
/// не чаще раза в календарный месяц — проверяется по <see cref="ISparkTransactionRepository.ExistsSinceAsync"/>
/// с началом текущего месяца (UTC), а не по признаку на User, т.к. история операций уже есть и не требует новой колонки.
/// </summary>
public sealed class CreateIdeaCommandHandler(
    IUserRepository userRepository,
    IIdeaRepository ideaRepository,
    ISparkTransactionRepository sparkTransactionRepository,
    ISparksService sparksService,
    IValidator<CreateIdeaCommand> validator,
    IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<CreateIdeaCommand, CreateIdeaResult>
{
    public async Task<CreateIdeaResult> Handle(CreateIdeaCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var idea = new Idea
        {
            Id = Guid.NewGuid(),
            AuthorUserId = request.UserId,
            AuthorUser = user,
            Text = request.Text,
            IsAnonymous = request.Anonymous,
            Status = IdeaStatus.New,
            VotesCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await ideaRepository.AddAsync(idea, cancellationToken);

        var monthStart = new DateTimeOffset(new DateTime(idea.CreatedAt.Year, idea.CreatedAt.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        var awardedThisMonth = await sparkTransactionRepository.ExistsSinceAsync(
            request.UserId, SparkTransactionType.IdeaSubmission, monthStart, cancellationToken);

        var sparksAwarded = 0;
        if (!awardedThisMonth)
        {
            sparksAwarded = sparksOptions.Value.IdeaSubmissionBonusAmount;
            await sparksService.AwardAsync(user, sparksAwarded, SparkTransactionType.IdeaSubmission, idea.Id, cancellationToken);
        }

        await ideaRepository.SaveChangesAsync(cancellationToken);

        return new CreateIdeaResult(
            idea.Id, idea.Text, idea.Status, idea.VotesCount, HasVoted: false,
            idea.IsAnonymous ? null : user.Name, IsMine: true, idea.CreatedAt, sparksAwarded);
    }
}
