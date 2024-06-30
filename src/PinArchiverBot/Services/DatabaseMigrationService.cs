using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PinArchiverBot.Persistence;

namespace PinArchiverBot.Services.Hosted;
internal class DatabaseMigrationService : BackgroundService
{
    private readonly ILogger<DatabaseMigrationService> _logger;
    private readonly IDbContextFactory<PinArchiverDbContext> _contextFactory;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public DatabaseMigrationService(
        ILogger<DatabaseMigrationService> logger,
        IDbContextFactory<PinArchiverDbContext> contextFactory,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogTrace("Starting database migration service.");

        try
        {
            using var context = _contextFactory.CreateDbContext();
            await context.Database.MigrateAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database.");
            _hostApplicationLifetime.StopApplication();
        }
    }
}
