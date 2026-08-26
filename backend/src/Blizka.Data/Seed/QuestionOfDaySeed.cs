using Blizka.App.Domain.Entities;

namespace Blizka.Data.Seed;

/// <summary>
/// Каталог вопросов дня (T-11.1) — джоба <c>GenerateQuestionOfDay</c> публикует по одному в день, по кругу,
/// когда каталог заканчивается (см. <see cref="Blizka.App.Domain.Repositories.IQuestionOfDayRepository.GetNextToPublishAsync"/>).
/// <c>PublishedAt</c> у всех <c>null</c> — простановка при первой публикации, не сидом.
/// </summary>
public static class QuestionOfDaySeed
{
    public static IReadOnlyList<QuestionOfDay> All { get; } =
    [
        Create(1, "Каким было ваше самое яркое путешествие и почему оно запомнилось?",
            "Якой была ваша самая яркая падарожжа і чаму яно запомнілася?",
            "What was your most memorable trip, and why did it stick with you?"),
        Create(2, "Если бы можно было выучить один навык за один день, что бы вы выбрали?",
            "Калі б можна было вывучыць адзін навык за адзін дзень, што б вы выбралі?",
            "If you could instantly master one skill, what would it be?"),
        Create(3, "Какая книга или фильм изменили ваш взгляд на жизнь?",
            "Якая кніга ці фільм змянілі ваш погляд на жыццё?",
            "What book or movie changed the way you see life?"),
        Create(4, "Что вы делаете, когда хотите отдохнуть от всех и всего?",
            "Што вы робіце, калі хочаце адпачыць ад усіх і ўсяго?",
            "What do you do when you want to switch off from everything?"),
        Create(5, "Какое ваше самое смелое решение за последний год?",
            "Якое ваша самае смелае рашэнне за апошні год?",
            "What's the boldest decision you've made in the last year?"),
        Create(6, "Если бы вы могли поужинать с любым человеком, живым или нет, кто бы это был?",
            "Калі б вы маглі павячэраць з любым чалавекам, жывым ці не, хто б гэта быў?",
            "If you could have dinner with anyone, dead or alive, who would it be?"),
        Create(7, "Какая привычка изменила вашу жизнь к лучшему?",
            "Якая звычка змяніла ваша жыццё да лепшага?",
            "What habit made the biggest positive difference in your life?"),
        Create(8, "Что вас искренне удивляет в людях?",
            "Што вас шчыра здзіўляе ў людзях?",
            "What genuinely surprises you about people?"),
        Create(9, "Какое место в мире вы мечтаете увидеть и почему?",
            "Якое месца ў свеце вы марыце ўбачыць і чаму?",
            "What place in the world do you dream of seeing, and why?"),
        Create(10, "Что для вас значит идеальные выходные?",
            "Што для вас значаць ідэальныя выхадныя?",
            "What does a perfect weekend look like for you?"),
        Create(11, "Какой совет вы бы дали себе пять лет назад?",
            "Якую параду вы б далі сабе пяць гадоў таму?",
            "What advice would you give yourself five years ago?"),
        Create(12, "Что вас вдохновляет заниматься тем, чем вы занимаетесь?",
            "Што вас натхняе займацца тым, чым вы займаецеся?",
            "What inspires you to do what you do?"),
        Create(13, "Какая мелочь способна поднять вам настроение в любой день?",
            "Якая дробязь здольная падняць вам настрой у любы дзень?",
            "What small thing can turn your whole day around?"),
        Create(14, "Если бы у вас была лишняя неделя свободного времени, на что бы вы её потратили?",
            "Калі б у вас быў лішні тыдзень вольнага часу, на што б вы яго патрацілі?",
            "If you had an extra free week, how would you spend it?"),
        Create(15, "Какая черта характера важнее всего для вас в других людях?",
            "Якая рыса характару важнейшая за ўсё для вас у іншых людзях?",
            "What personality trait matters most to you in other people?"),
        Create(16, "Что вы считаете своим главным достижением на сегодня?",
            "Што вы лічыце сваім галоўным дасягненнем на сёння?",
            "What do you consider your biggest achievement so far?"),
        Create(17, "Какую традицию вы бы хотели завести в своей жизни?",
            "Якую традыцыю вы б хацелі завесці ў сваім жыцці?",
            "What tradition would you like to start in your life?"),
        Create(18, "Что вас пугает, но при этом привлекает?",
            "Што вас палохае, але пры гэтым вабіць?",
            "What scares you but excites you at the same time?"),
        Create(19, "Какое блюдо вы могли бы есть каждый день и не устать?",
            "Якую страву вы маглі б есці кожны дзень і не стаміцца?",
            "What dish could you eat every day and never get tired of?"),
        Create(20, "Что бы вы хотели, чтобы люди понимали о вас с первого взгляда?",
            "Што б вы хацелі, каб людзі разумелі пра вас з першага погляду?",
            "What do you wish people understood about you right away?"),
    ];

    private static QuestionOfDay Create(int index, string textRu, string textBe, string textEn) => new()
    {
        Id = Guid.Parse($"00000000-0000-0000-0b11-{index:D12}"),
        TextRu = textRu,
        TextBe = textBe,
        TextEn = textEn,
        PublishedAt = null,
        CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(index),
    };
}
