namespace Blizka.App.UseCases.Matches;

/// <summary>Второй участник мэтча в списках T-7.1 — общая проекция для всех трёх секций.</summary>
public sealed record MatchUserResult(Guid UserId, string Name, int Age, string? MainPhotoUrl);
