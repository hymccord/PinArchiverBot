using System.Text;

using Discord;
using Discord.Interactions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using PinArchiverBot.Persistence;
using PinArchiverBot.Services;

namespace PinArchiverBot.SlashCommands;

[RequireContext(ContextType.Guild)]
[RequireOwner(Group = "Permission")]
[RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
[Group("pinarchive", "Commands for configuring archiving.")]
public class PinArchiveCommand : InteractionModuleBase
{
    private readonly ILogger<PinArchiveCommand> _logger;
    private readonly IPinArchiverService _archiverService;
    private readonly IDbContextFactory<PinArchiverDbContext> _contextFactory;

    public PinArchiveCommand(ILogger<PinArchiveCommand> logger, IPinArchiverService archiverService, IDbContextFactory<PinArchiverDbContext> contextFactory)
    {
        _logger = logger;
        _archiverService = archiverService;
        _contextFactory = contextFactory;
    }

    [SlashCommand("enable", "Enable pinned messages to be archived to specified channel.")]
    public async Task EnableAsync([Summary("Channel", "The text channel to archive pins to.")][ChannelTypes(ChannelType.Text)] IGuildChannel archiveChannel)
    {
        _logger.LogInformation("Enabling archiving of pinned messages to {Channel}.", archiveChannel.Name);

        await DeferAsync();
        await _archiverService.EnableArchiveChannelAsync(Context.Guild.Id, archiveChannel.Id);
        await FollowupAsync($"Archiving to <#{archiveChannel.Id}>");
    }

    [SlashCommand("disable", "Disable archiving of pinned messages.")]
    public async Task DisableAsync()
    {
        _logger.LogInformation("Disabling archiving of pinned messages.");

        await DeferAsync();
        await _archiverService.DisableArchiveChannelAsync(Context.Guild.Id);
        await FollowupAsync("Disabled archiving.");
    }

    [SlashCommand("exclude", "Exclude a channel from archiving.")]
    public async Task ExcludeAsync([Summary("Channel", "The text channel to exclude from archiving.")][ChannelTypes(ChannelType.Text)] IGuildChannel archiveChannel)
    {
        _logger.LogInformation("Excluding channel {Channel} from archiving.", archiveChannel.Name);

        await DeferAsync();
        await _archiverService.BlacklistChannelAsync(Context.Guild.Id, archiveChannel.Id);
        await FollowupAsync($"Excluded <#{archiveChannel.Id}> from archiving.");
    }

    [SlashCommand("include", "Remove a channel from the exclude list.")]
    public async Task IncludeAsync([Summary("Channel", "The text channel to include in archiving.")][ChannelTypes(ChannelType.Text)] IGuildChannel archiveChannel)
    {
        _logger.LogInformation("Including channel {Channel} in archiving.", archiveChannel.Name);

        await DeferAsync();
        await _archiverService.WhitelistChannelAsync(Context.Guild.Id, archiveChannel.Id);
        await FollowupAsync($"Included <#{archiveChannel.Id}> in archiving.");
    }

    [SlashCommand("archive", "Archive the pinned messages in the specified channel.")]
    public async Task ArchiveAsync([Summary("Channel", "The text channel to archive pins from.")][ChannelTypes(ChannelType.Text)] IGuildChannel archiveChannel)
    {
        _logger.LogInformation("Archiving pinned messages from {Channel}.", archiveChannel.Name);

        await DeferAsync();
        await _archiverService.ArchiveChannelAsync(archiveChannel.Id);
        await FollowupAsync($"Archived pinned messages from <#{archiveChannel.Id}>.");
    }

    [SlashCommand("list", "List the current archive settings.")]
    public async Task ListAsync()
    {
        _logger.LogInformation("Listing archive settings.");

        await DeferAsync();

        var context = _contextFactory.CreateDbContext();

        var archiveChannel = await context.ArchiveChannels.FindAsync(Context.Guild.Id);
        var blacklistChannels = await context.BlacklistChannels
            .Where(bc => bc.GuildId == Context.Guild.Id)
            .ToListAsync();

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Settings")
            .AddField(":file_cabinet: ", archiveChannel is null ? "Not set" : $"<#{archiveChannel.ChannelId}>");

        if (blacklistChannels.Count != 0)
        {
            var sb = new StringBuilder();
            foreach (var channel in blacklistChannels)
            {
                sb.AppendLine($":x: <#{channel.ChannelId}>");
            }
            embedBuilder.AddField("Excluded channels", sb.ToString());
        }

        await FollowupAsync(embed: embedBuilder.Build());
    }

}
