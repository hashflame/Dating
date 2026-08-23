namespace Blizka.App.Domain.Enums;

public enum MatchStatus
{
    Active,
    Archived,

    /// <summary>Заведено только как значение enum (spec 002, B10) — без API/побочных эффектов, пока не решён unmatch-флоу.</summary>
    Unmatched,
}
