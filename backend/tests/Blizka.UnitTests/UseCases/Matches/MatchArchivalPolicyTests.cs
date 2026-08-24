using Blizka.App.UseCases.Matches;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class MatchArchivalPolicyTests
{
    [Fact(DisplayName = "КОГДА контакт не открыт и с мэтча прошло больше 7 дней ТОГДА мэтч протух")]
    public void IsStale_returns_true_for_a_matched_but_unopened_match_older_than_7_days()
    {
        var now = DateTimeOffset.UtcNow;
        var matchedAt = now.AddDays(-8);

        Assert.True(MatchArchivalPolicy.IsStale(matchedAt, contactUnlockedAt: null, messageSentCheckAt: null, now));
    }

    [Fact(DisplayName = "КОГДА контакт не открыт и с мэтча прошло меньше 7 дней ТОГДА мэтч не протух")]
    public void IsStale_returns_false_for_a_recent_unopened_match()
    {
        var now = DateTimeOffset.UtcNow;
        var matchedAt = now.AddDays(-6);

        Assert.False(MatchArchivalPolicy.IsStale(matchedAt, contactUnlockedAt: null, messageSentCheckAt: null, now));
    }

    [Fact(DisplayName = "КОГДА контакт открыт больше 7 дней назад и message-sent-check не проставлен ТОГДА мэтч протух")]
    public void IsStale_returns_true_for_an_unlocked_match_without_message_sent_check_older_than_7_days()
    {
        var now = DateTimeOffset.UtcNow;
        var matchedAt = now.AddDays(-30);
        var contactUnlockedAt = now.AddDays(-8);

        Assert.True(MatchArchivalPolicy.IsStale(matchedAt, contactUnlockedAt, messageSentCheckAt: null, now));
    }

    [Fact(DisplayName = "КОГДА контакт открыт больше 7 дней назад, но message-sent-check уже проставлен ТОГДА мэтч не протух")]
    public void IsStale_returns_false_for_an_unlocked_match_with_message_sent_check()
    {
        var now = DateTimeOffset.UtcNow;
        var matchedAt = now.AddDays(-30);
        var contactUnlockedAt = now.AddDays(-8);
        var messageSentCheckAt = now.AddDays(-8);

        Assert.False(MatchArchivalPolicy.IsStale(matchedAt, contactUnlockedAt, messageSentCheckAt, now));
    }
}
