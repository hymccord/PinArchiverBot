using Microsoft.EntityFrameworkCore;

using PinArchiverBot.Persistence.Models;

namespace PinArchiverBot.Persistence;
internal class PinArchiverDbContext(DbContextOptions<PinArchiverDbContext> options) : DbContext(options)
{
    public DbSet<ArchiveChannel> ArchiveChannels { get; set; }
}
