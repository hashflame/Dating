namespace Blizka.App.Domain.Enums;

/// <summary>
/// Четыре вкладки доски идей (T-19.1, S-60) — decomposition.md описывает только <c>?sort=hot|new</c>, но макет
/// добавляет ещё «В работе» (статусы <see cref="Blizka.App.Domain.Entities.Idea.Status"/> underReview+planned) и
/// «Мои» (идеи текущего пользователя), которые на клиенте не отфильтровать — список постраничный (тикет ClickUp).
/// </summary>
public enum IdeaListTab
{
    Hot,
    New,
    InWork,
    Mine,
}
