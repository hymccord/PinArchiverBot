using Microsoft.EntityFrameworkCore;

using PinArchiverBot.Persistence.Models;

namespace PinArchiverBot.Persistence;
public class PinArchiverDbContext(DbContextOptions<PinArchiverDbContext> options) : DbContext(options)
{
    public DbSet<ArchiveChannel> ArchiveChannels { get; set; }
    public DbSet<BlacklistChannel> BlacklistChannels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BlacklistChannel>(bc =>
        {
            bc.HasKey(bc => new { bc.GuildId, bc.ChannelId });
            bc.HasIndex(bc => bc.GuildId);
            bc.HasIndex(bc => bc.ChannelId).IsUnique();
        });
    }
}
