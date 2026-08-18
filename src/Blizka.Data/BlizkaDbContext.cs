using Blizka.App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data;

public sealed class BlizkaDbContext(DbContextOptions<BlizkaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Photo> Photos => Set<Photo>();

    public DbSet<Interest> Interests => Set<Interest>();

    public DbSet<UserInterest> UserInterests => Set<UserInterest>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<Swipe> Swipes => Set<Swipe>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<SparkTransaction> SparkTransactions => Set<SparkTransaction>();

    public DbSet<QuestionOfDay> QuestionsOfDay => Set<QuestionOfDay>();

    public DbSet<QuestionAnswer> QuestionAnswers => Set<QuestionAnswer>();

    public DbSet<Minigame> Minigames => Set<Minigame>();

    public DbSet<MinigameAnswer> MinigameAnswers => Set<MinigameAnswer>();

    public DbSet<Idea> Ideas => Set<Idea>();

    public DbSet<IdeaVote> IdeaVotes => Set<IdeaVote>();

    public DbSet<DatePreference> DatePreferences => Set<DatePreference>();

    public DbSet<UserDatePreference> UserDatePreferences => Set<UserDatePreference>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<TelegramPayment> TelegramPayments => Set<TelegramPayment>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<CityWaitlist> CityWaitlists => Set<CityWaitlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssemblyMarker).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
