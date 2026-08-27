using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Обрабатывает <see cref="GetDateIdeasQuery"/> (T-12.1) — MVP-заглушка: реальная LLM-генерация ждёт T-13.1
/// (см. <see cref="DateIdeaCatalog"/>). В подборе участвует только пересечение <c>DatePreference</c> обоих
/// участников (T-9.3) — общие интересы (T-9.2), которые decomposition.md тоже упоминает как вход алгоритма,
/// пока не учитываются: это часть настоящей LLM-генерации, а не подбора по фиксированному каталогу.
/// </summary>
public sealed class GetDateIdeasQueryHandler(IMatchRepository matchRepository) : IRequestHandler<GetDateIdeasQuery, DateIdeasResult>
{
    private const int MaxIdeas = 3;
    private const int MinIdeas = 2;

    public async Task<DateIdeasResult> Handle(GetDateIdeasQuery request, CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdForUserAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        var (me, other) = MatchResultMapper.ResolveUsers(match, request.UserId);

        var sharedPreferenceCodes = me.UserDatePreferences.Select(p => p.DatePreference!.Code)
            .Intersect(other.UserDatePreferences.Select(p => p.DatePreference!.Code))
            .ToHashSet();

        // Город берётся с сервера (City обоих участников мэтча), а не из query-параметра запроса — клиент не
        // знает, в каком падеже его отдавать, а шаблоны каталога ждут ровно предложный падеж (см.
        // CityLocativeNames). request.City больше не используется (тикет ClickUp): подстановка сырого имени
        // города решала бы одну проблему («вашем городе» вместо реального названия) и создавала другую —
        // рассогласование падежа («по центру Минск» вместо «по центру Минска»).
        var city = CityLocativeNames.ForInPhrase(me.City?.NameRu ?? other.City?.NameRu);
        // Только для решения, применять ли фильтр по maxBudget (см. DateIdeaCatalog.Pick) — ответ всегда
        // подписан DateIdeaCatalog.DefaultCurrency, конвертации валют в заглушке нет.
        var requestedCurrency = string.IsNullOrWhiteSpace(request.Currency) ? DateIdeaCatalog.DefaultCurrency : request.Currency.ToUpperInvariant();

        var ideas = DateIdeaCatalog.Pick(sharedPreferenceCodes, request.MaxBudget, city, requestedCurrency, MaxIdeas, MinIdeas);

        return new DateIdeasResult(ideas);
    }
}
