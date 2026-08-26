using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class QuestionAnswerRepository(BlizkaDbContext dbContext) : IQuestionAnswerRepository
{
    private const string AnswerUniqueConstraintName = "IX_QuestionAnswers_MatchId_QuestionId_UserId";
    public async Task<IReadOnlyList<QuestionAnswer>> GetByMatchAndQuestionAsync(
        Guid matchId, Guid questionId, CancellationToken cancellationToken) =>
        await dbContext.QuestionAnswers
            .AsNoTracking()
            .Where(a => a.MatchId == matchId && a.QuestionId == questionId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<QuestionAnswer>> GetByMatchAndQuestionsAsync(
        Guid matchId, IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken) =>
        await dbContext.QuestionAnswers
            .AsNoTracking()
            .Where(a => a.MatchId == matchId && questionIds.Contains(a.QuestionId))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(QuestionAnswer answer, CancellationToken cancellationToken) =>
        await dbContext.QuestionAnswers.AddAsync(answer, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsAnswerUniqueViolation(ex))
        {
            var conflictingAnswer = dbContext.ChangeTracker.Entries<QuestionAnswer>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(answer => dbContext.Entry(answer).State == EntityState.Added);

            throw new QuestionAnswerConflictException(conflictingAnswer?.MatchId ?? Guid.Empty, ex);
        }
    }

    private static bool IsAnswerUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        postgresException.ConstraintName == AnswerUniqueConstraintName;
}
