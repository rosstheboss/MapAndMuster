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
            entity.Property(campaign => campaign.MapStorageKey).HasMaxLength(260);
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
        });

        builder.Entity<CampaignMembershipRecord>(entity =>
        {
            entity.ToTable("CampaignMemberships");
            entity.HasKey(membership => membership.Id);
            entity.HasIndex(membership => new { membership.CampaignId, membership.UserId }).IsUnique();
            entity.HasIndex(membership => membership.UserId);
        });

        builder.Entity<CampaignAllyGroupRecord>(entity =>
        {
            entity.ToTable("CampaignAllyGroups");
            entity.HasKey(group => group.Id);
            entity.Property(group => group.Name).HasMaxLength(60).IsRequired();
        });

        builder.Entity<CampaignFactionRecord>(entity =>
        {
            entity.ToTable("CampaignFactions");
            entity.HasKey(faction => faction.Id);
            entity.Property(faction => faction.Name).HasMaxLength(60).IsRequired();
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
    }
}
