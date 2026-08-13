using Campaign.Infrastructure.Email;
using Campaign.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for identity, profiles, and the email outbox.
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
    }
}
