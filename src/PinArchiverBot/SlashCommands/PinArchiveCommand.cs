using Discord;
using Discord.Interactions;

using Microsoft.Extensions.Logging;

namespace PinArchiverBot.SlashCommands;

[RequireContext(ContextType.Guild)]
[RequireOwner(Group = "Permission")]
[RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
[Group("pinarchive", "Commands for configuring archiving.")]
public class PinArchiveCommand : InteractionModuleBase
{
    private readonly ILogger<PinArchiveCommand> _logger;
    private readonly IPinArchiverService _archiverService;

    public PinArchiveCommand(ILogger<PinArchiveCommand> logger, IPinArchiverService archiverService)
    {
        _logger = logger;
        _archiverService = archiverService;
    }

    [SlashCommand("enable", "Enable archiving of pinned messages to specified channel.")]
    public async Task EnableAsync([Summary("Channel", "The text channel to archive pins to.")][ChannelTypes(ChannelType.Text)] IGuildChannel archiveChannel)
    {
        _logger.LogInformation("Enabling archiving of pinned messages to {Channel}.", archiveChannel.Name);

        await DeferAsync(ephemeral: true);
        await _archiverService.EnableArchiveChannelAsync(Context.Guild.Id, archiveChannel.Id);
        await FollowupAsync($"Archiving to <#{archiveChannel.Id}>", ephemeral: true);
    }

    [SlashCommand("disable", "Disable archiving of pinned messages.")]
    public async Task DisableAsync()
    {
        _logger.LogInformation("Disabling archiving of pinned messages.");

        await DeferAsync(ephemeral: true);        
        await _archiverService.DisableArchiveChannelAsync(Context.Guild.Id);
        await FollowupAsync("Disabled archiving.", ephemeral: true);
    }
}
