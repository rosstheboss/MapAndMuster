using Campaign.Infrastructure.Email;
using Campaign.Infrastructure.Identity;
using Campaign.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for identity, profiles, campaigns, and the email outbox.
/// </summary>
public sealed class CampaignDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Initializes a new context.
    /// </summary>
    /// <param name="options">The context options.</param>
    public CampaignDbContext(DbContextOptions<CampaignDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the transactional email outbox.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Gets the campaigns.
    /// </summary>
    public DbSet<CampaignRecord> Campaigns => Set<CampaignRecord>();

    /// <summary>
    /// Gets in-app user notifications.
    /// </summary>
    public DbSet<UserNotificationRecord> UserNotifications => Set<UserNotificationRecord>();

    /// <summary>
    /// Gets site-wide news articles.
    /// </summary>
    public DbSet<NewsArticleRecord> NewsArticles => Set<NewsArticleRecord>();

    /// <summary>
    /// Gets public site-wide chat messages.
    /// </summary>
    public DbSet<SiteChatMessageRecord> SiteChatMessages => Set<SiteChatMessageRecord>();

    /// <summary>
    /// Gets directed site-chat blocks.
    /// </summary>
    public DbSet<SiteChatBlockRecord> SiteChatBlocks => Set<SiteChatBlockRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FirstName).HasMaxLength(50).IsRequired();
            entity.Property(user => user.MiddleInitial).HasMaxLength(1);
            entity.Property(user => user.LastName).HasMaxLength(50).IsRequired();
            entity.Property(user => user.Suffix).HasMaxLength(8);
            entity.Property(user => user.City).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Region).HasMaxLength(100);
            entity.Property(user => user.Country).HasMaxLength(100).IsRequired();
            entity.Property(user => user.TimeZoneId).HasMaxLength(64);
            entity.Property(user => user.AvatarStorageKey).HasMaxLength(260);
            entity.Property(user => user.DisplayNameMode).HasConversion<string>().HasMaxLength(32);
            entity.Property(user => user.InAppNotificationsEnabled).IsRequired().HasDefaultValue(true);
            entity.Property(user => user.EmailNotificationsEnabled).IsRequired().HasDefaultValue(true);
            entity.Property(user => user.PreferredChatLanguage).HasMaxLength(32).IsRequired().HasDefaultValue("English");
            entity.Property(user => user.ProfileRevision).IsConcurrencyToken();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        builder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Type).HasMaxLength(128).IsRequired();
            entity.Property(message => message.Payload).IsRequired();
            entity.HasIndex(message => message.ProcessedUtc);
        });

        builder.Entity<CampaignRecord>(entity =>
        {
            entity.ToTable("Campaigns");
            entity.HasKey(campaign => campaign.Id);
            entity.Property(campaign => campaign.Name).HasMaxLength(80).IsRequired();
            entity.Property(campaign => campaign.Description).HasMaxLength(500);
            entity.Property(campaign => campaign.JoinPasswordHash).HasMaxLength(500);
            entity.Property(campaign => campaign.City).HasMaxLength(100);
            entity.Property(campaign => campaign.Region).HasMaxLength(100);
            entity.Property(campaign => campaign.Country).HasMaxLength(100);
            entity.Property(campaign => campaign.MapStorageKey).HasMaxLength(260);
            entity.Property(campaign => campaign.MapGraphJson).HasColumnType("jsonb");
            entity.Property(campaign => campaign.PlayStateJson).HasColumnType("jsonb");
            entity.Property(campaign => campaign.CatalogJson).HasColumnType("jsonb");
            entity.Property(campaign => campaign.TimeZoneId).HasMaxLength(64).IsRequired();
            entity.Property(campaign => campaign.RoundLengthUnit).HasMaxLength(16).IsRequired();
            entity.Property(campaign => campaign.Revision).IsConcurrencyToken().ValueGeneratedNever();
            entity.HasIndex(campaign => campaign.CreatedByUserId);
            entity.HasMany(campaign => campaign.Memberships)
                .WithOne(membership => membership.Campaign)
                .HasForeignKey(membership => membership.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(campaign => campaign.AllyGroups)
                .WithOne(group => group.Campaign)
                .HasForeignKey(group => group.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(campaign => campaign.Factions)
                .WithOne(faction => faction.Campaign)
                .HasForeignKey(faction => faction.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(campaign => campaign.Links)
                .WithOne(link => link.Campaign)
                .HasForeignKey(link => link.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(campaign => campaign.Phases)
                .WithOne(phase => phase.Campaign)
                .HasForeignKey(phase => phase.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CampaignMembershipRecord>(entity =>
        {
            entity.ToTable("CampaignMemberships");
            entity.HasKey(membership => membership.Id);
            entity.HasIndex(membership => new { membership.CampaignId, membership.UserId }).IsUnique();
            entity.HasIndex(membership => membership.UserId);
            entity.Property(membership => membership.Subfaction).HasMaxLength(60);
        });

        builder.Entity<CampaignAllyGroupRecord>(entity =>
        {
            entity.ToTable("CampaignAllyGroups");
            entity.HasKey(group => group.Id);
            entity.Property(group => group.Name).HasMaxLength(60).IsRequired();
            entity.Property(group => group.Color).HasMaxLength(7).IsRequired();
        });

        builder.Entity<CampaignFactionRecord>(entity =>
        {
            entity.ToTable("CampaignFactions");
            entity.HasKey(faction => faction.Id);
            entity.Property(faction => faction.Name).HasMaxLength(60).IsRequired();
            entity.Property(faction => faction.Color).HasMaxLength(7).IsRequired();
            entity.Property(faction => faction.RequiresSubfaction).IsRequired();
            entity.Property(faction => faction.FlagImageStorageKey).HasMaxLength(260);
            entity.HasOne(faction => faction.AllyGroup)
                .WithMany(group => group.Factions)
                .HasForeignKey(faction => faction.AllyGroupId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(faction => faction.Subfactions)
                .WithOne(subfaction => subfaction.Faction)
                .HasForeignKey(subfaction => subfaction.FactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CampaignSubfactionRecord>(entity =>
        {
            entity.ToTable("CampaignSubfactions");
            entity.HasKey(subfaction => subfaction.Id);
            entity.Property(subfaction => subfaction.Name).HasMaxLength(60).IsRequired();
        });

        builder.Entity<CampaignLinkRecord>(entity =>
        {
            entity.ToTable("CampaignLinks");
            entity.HasKey(link => link.Id);
            entity.Property(link => link.Label).HasMaxLength(80).IsRequired();
            entity.Property(link => link.Url).HasMaxLength(2048).IsRequired();
        });

        builder.Entity<CampaignRoundPhaseRecord>(entity =>
        {
            entity.ToTable("CampaignRoundPhases");
            entity.HasKey(phase => phase.Id);
            entity.Property(phase => phase.Kind).HasMaxLength(16).IsRequired();
            entity.Property(phase => phase.DurationUnit).HasMaxLength(16).IsRequired();
            entity.HasIndex(phase => phase.CampaignId);
        });

        builder.Entity<UserNotificationRecord>(entity =>
        {
            entity.ToTable("UserNotifications");
            entity.HasKey(notice => notice.Id);
            entity.Property(notice => notice.Kind).HasMaxLength(32).IsRequired();
            entity.Property(notice => notice.CampaignName).HasMaxLength(80);
            entity.Property(notice => notice.Title).HasMaxLength(160).IsRequired();
            entity.Property(notice => notice.Body).HasMaxLength(500).IsRequired();
            entity.Property(notice => notice.Path).HasMaxLength(260).IsRequired();
            entity.Property(notice => notice.DedupeKey).HasMaxLength(160).IsRequired();
            entity.HasIndex(notice => new { notice.UserId, notice.DedupeKey }).IsUnique();
            entity.HasIndex(notice => new { notice.UserId, notice.ReadUtc, notice.CreatedUtc });
        });

        builder.Entity<NewsArticleRecord>(entity =>
        {
            entity.ToTable("NewsArticles");
            entity.HasKey(article => article.Id);
            entity.Property(article => article.Title).HasMaxLength(120).IsRequired();
            entity.Property(article => article.BodyMarkdown).HasMaxLength(20000).IsRequired();
            entity.HasIndex(article => article.PublishedUtc);
        });

        builder.Entity<SiteChatMessageRecord>(entity =>
        {
            entity.ToTable("SiteChatMessages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.AuthorUsername).HasMaxLength(32).IsRequired();
            entity.Property(message => message.AuthorDisplayName).HasMaxLength(160).IsRequired();
            entity.Property(message => message.Body).HasMaxLength(2000).IsRequired();
            entity.Property(message => message.Language).HasMaxLength(32).IsRequired();
            entity.Property(message => message.Kind).HasMaxLength(16).IsRequired();
            entity.Property(message => message.TargetUsername).HasMaxLength(32);
            entity.Property(message => message.TargetDisplayName).HasMaxLength(160);
            entity.HasIndex(message => message.PostedUtc);
        });

        builder.Entity<SiteChatBlockRecord>(entity =>
        {
            entity.ToTable("SiteChatBlocks");
            entity.HasKey(block => new { block.BlockerUserId, block.BlockedUserId });
            entity.HasIndex(block => block.BlockedUserId);
        });
    }
}
