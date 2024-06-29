using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;

using Microsoft.Extensions.Logging;

namespace PinArchiverBot.SlashCommands;

[RequireOwner(Group = "Permission")]
[RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
[Group("pinarchive", "Commands for configuring archiving.")]
public class PinArchiveCommand : InteractionModuleBase
{
    private readonly ILogger<PinArchiveCommand> _logger;

    public PinArchiveCommand(ILogger<PinArchiveCommand> logger)
    {
        _logger = logger;
    }

    [SlashCommand("enable", "Enable archiving of pinned messages to specified channel.")]
    public async Task EnableAsync([Summary("Channel", "The text channel to archive pins to.")][ChannelTypes(ChannelType.Text)] IChannel archiveChannel)
    {
        _logger.LogInformation("Enabling archiving of pinned messages to {Channel}.", archiveChannel.Name);
    }

    [SlashCommand("disable", "Disable archiving of pinned messages.")]
    public async Task DisableAsync()
    {
        _logger.LogInformation("Disabling archiving of pinned messages.");
    }
}
