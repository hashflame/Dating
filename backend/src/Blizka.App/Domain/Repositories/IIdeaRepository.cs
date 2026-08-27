using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Repositories;

/// <summary>Доступ к данным доски идей (T-19.1) — поверх <c>Idea</c>/<c>IdeaVote</c>.</summary>
public interface IIdeaRepository
{
    /// <summary>Страница идей для вкладки <paramref name="tab"/>, с отметкой <see cref="IdeaListEntry.HasVoted"/> для <paramref name="currentUserId"/>.</summary>
    Task<(IReadOnlyList<IdeaListEntry> Items, int TotalCount)> GetPageAsync(
        IdeaListTab tab, Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid ideaId, CancellationToken cancellationToken);

    Task AddAsync(Idea idea, CancellationToken cancellationToken);

    /// <summary>
    /// Ставит голос пользователя за идею. Сама по себе транзакционно согласована (вставка голоса и инкремент
    /// <c>Idea.VotesCount</c> в БД) — не требует отдельного <see cref="SaveChangesAsync"/>.
    /// </summary>
    /// <returns><c>false</c>, если пользователь уже голосовал за эту идею — идемпотентный no-op, счётчик не меняется.</returns>
    Task<bool> AddVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken);

    /// <summary>Снимает голос пользователя с идеи — тоже самодостаточна, как и <see cref="AddVoteAsync"/>.</summary>
    /// <returns><c>false</c>, если голоса не было — идемпотентный no-op, счётчик не меняется.</returns>
    Task<bool> RemoveVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken);

    /// <summary>Сохраняет только <see cref="AddAsync"/> (создание идеи вместе с начислением зорок в том же вызывающем хендлере).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Идея вместе с отметкой, голосовал ли за неё <c>currentUserId</c> из <see cref="IIdeaRepository.GetPageAsync"/>.</summary>
public sealed record IdeaListEntry(Idea Idea, bool HasVoted);
