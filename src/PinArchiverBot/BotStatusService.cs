using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;

using Microsoft.Extensions.Logging;

namespace PinArchiverBot;
internal class BotStatusService : DiscordClientService
{
    public BotStatusService(DiscordSocketClient client, ILogger<BotStatusService> logger)
        : base(client, logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Client.WaitForReadyAsync(stoppingToken).ConfigureAwait(false);
        await Client.SetActivityAsync(new Game("for pinned messages.", ActivityType.Watching)).ConfigureAwait(false);
    }
}
