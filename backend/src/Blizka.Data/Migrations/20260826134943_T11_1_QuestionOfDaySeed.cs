using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class T11_1_QuestionOfDaySeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "QuestionsOfDay",
                columns: new[] { "Id", "CreatedAt", "PublishedAt", "TextBe", "TextEn", "TextRu" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0b11-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 1, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якой была ваша самая яркая падарожжа і чаму яно запомнілася?", "What was your most memorable trip, and why did it stick with you?", "Каким было ваше самое яркое путешествие и почему оно запомнилось?" },
                    { new Guid("00000000-0000-0000-0b11-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 2, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Калі б можна было вывучыць адзін навык за адзін дзень, што б вы выбралі?", "If you could instantly master one skill, what would it be?", "Если бы можно было выучить один навык за один день, что бы вы выбрали?" },
                    { new Guid("00000000-0000-0000-0b11-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 3, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якая кніга ці фільм змянілі ваш погляд на жыццё?", "What book or movie changed the way you see life?", "Какая книга или фильм изменили ваш взгляд на жизнь?" },
                    { new Guid("00000000-0000-0000-0b11-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 4, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Што вы робіце, калі хочаце адпачыць ад усіх і ўсяго?", "What do you do when you want to switch off from everything?", "Что вы делаете, когда хотите отдохнуть от всех и всего?" },
                    { new Guid("00000000-0000-0000-0b11-000000000005"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 5, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якое ваша самае смелае рашэнне за апошні год?", "What's the boldest decision you've made in the last year?", "Какое ваше самое смелое решение за последний год?" },
                    { new Guid("00000000-0000-0000-0b11-000000000006"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 6, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Калі б вы маглі павячэраць з любым чалавекам, жывым ці не, хто б гэта быў?", "If you could have dinner with anyone, dead or alive, who would it be?", "Если бы вы могли поужинать с любым человеком, живым или нет, кто бы это был?" },
                    { new Guid("00000000-0000-0000-0b11-000000000007"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 7, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якая звычка змяніла ваша жыццё да лепшага?", "What habit made the biggest positive difference in your life?", "Какая привычка изменила вашу жизнь к лучшему?" },
                    { new Guid("00000000-0000-0000-0b11-000000000008"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 8, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Што вас шчыра здзіўляе ў людзях?", "What genuinely surprises you about people?", "Что вас искренне удивляет в людях?" },
                    { new Guid("00000000-0000-0000-0b11-000000000009"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 9, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якое месца ў свеце вы марыце ўбачыць і чаму?", "What place in the world do you dream of seeing, and why?", "Какое место в мире вы мечтаете увидеть и почему?" },
                    { new Guid("00000000-0000-0000-0b11-000000000010"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 10, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Што для вас значаць ідэальныя выхадныя?", "What does a perfect weekend look like for you?", "Что для вас значит идеальные выходные?" },
                    { new Guid("00000000-0000-0000-0b11-000000000011"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 11, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якую параду вы б далі сабе пяць гадоў таму?", "What advice would you give yourself five years ago?", "Какой совет вы бы дали себе пять лет назад?" },
                    { new Guid("00000000-0000-0000-0b11-000000000012"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 12, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Што вас натхняе займацца тым, чым вы займаецеся?", "What inspires you to do what you do?", "Что вас вдохновляет заниматься тем, чем вы занимаетесь?" },
                    { new Guid("00000000-0000-0000-0b11-000000000013"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 13, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якая дробязь здольная падняць вам настрой у любы дзень?", "What small thing can turn your whole day around?", "Какая мелочь способна поднять вам настроение в любой день?" },
                    { new Guid("00000000-0000-0000-0b11-000000000014"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 14, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Калі б у вас быў лішні тыдзень вольнага часу, на што б вы яго патрацілі?", "If you had an extra free week, how would you spend it?", "Если бы у вас была лишняя неделя свободного времени, на что бы вы её потратили?" },
                    { new Guid("00000000-0000-0000-0b11-000000000015"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якая рыса характару важнейшая за ўсё для вас у іншых людзях?", "What personality trait matters most to you in other people?", "Какая черта характера важнее всего для вас в других людях?" },
                    { new Guid("00000000-0000-0000-0b11-000000000016"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 16, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Што вы лічыце сваім галоўным дасягненнем на сёння?", "What do you consider your biggest achievement so far?", "Что вы считаете своим главным достижением на сегодня?" },
                    { new Guid("00000000-0000-0000-0b11-000000000017"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 17, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якую традыцыю вы б хацелі завесці ў сваім жыцці?", "What tradition would you like to start in your life?", "Какую традицию вы бы хотели завести в своей жизни?" },
                    { new Guid("00000000-0000-0000-0b11-000000000018"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 18, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Што вас палохае, але пры гэтым вабіць?", "What scares you but excites you at the same time?", "Что вас пугает, но при этом привлекает?" },
                    { new Guid("00000000-0000-0000-0b11-000000000019"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 19, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Якую страву вы маглі б есці кожны дзень і не стаміцца?", "What dish could you eat every day and never get tired of?", "Какое блюдо вы могли бы есть каждый день и не устать?" },
                    { new Guid("00000000-0000-0000-0b11-000000000020"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Што б вы хацелі, каб людзі разумелі пра вас з першага погляду?", "What do you wish people understood about you right away?", "Что бы вы хотели, чтобы люди понимали о вас с первого взгляда?" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionsOfDay_PublishedAt",
                table: "QuestionsOfDay",
                column: "PublishedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestionsOfDay_PublishedAt",
                table: "QuestionsOfDay");

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000001"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000002"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000003"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000004"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000005"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000006"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000007"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000008"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000009"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000010"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000011"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000012"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000013"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000014"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000015"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000016"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000017"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000018"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000019"));

            migrationBuilder.DeleteData(
                table: "QuestionsOfDay",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0b11-000000000020"));
        }
    }
}
