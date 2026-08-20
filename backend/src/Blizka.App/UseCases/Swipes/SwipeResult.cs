using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Swipes;

public sealed record SwipeResult(SwipeType Action, bool IsMatch, MatchResult? Match, int SparksBalance);

/// <summary>Данные мэтча для экрана S-16 — показывается только когда лайк оказался взаимным.</summary>
public sealed record MatchResult(Guid MatchId, Guid UserId, string Name, IReadOnlyList<IcebreakerResult> Icebreakers);

/// <summary>Один из трёх лёгких входов для начала общения (S-16, notes) — фиксированный набор, см. <see cref="IcebreakerCatalog"/>.</summary>
public sealed record IcebreakerResult(string Type, string Label, string Effort);
