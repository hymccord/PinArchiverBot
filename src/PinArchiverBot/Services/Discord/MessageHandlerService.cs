using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;

using Microsoft.Extensions.Logging;

using PinArchiverBot.Services;

namespace PinArchiverBot.Services.Discord;
internal class MessageHandlerService : DiscordClientService
{
    private readonly PinArchiverService _archiverService;

    public MessageHandlerService(DiscordSocketClient client, ILogger<DiscordClientService> logger, PinArchiverService archiverService)
        : base(client, logger)
    {
        _archiverService = archiverService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _archiverService.InitializeAsync().ConfigureAwait(false);
        await Client.WaitForReadyAsync(stoppingToken).ConfigureAwait(false);
        Logger.LogInformation("MessageHandlerService is now running.");

        Client.MessageUpdated += HandleUpdatedMessage;
    }

    private async Task HandleUpdatedMessage(Cacheable<IMessage?, ulong> messageBefore, SocketMessage messageAfter, ISocketMessageChannel channel)
    {
        Logger.LogDebug("Updated message: {Message}", messageAfter.Content);

        if (channel is SocketGuildChannel guildChannel &&
            messageAfter is IUserMessage userMessage)
        {
            await _archiverService.OnMessageEditedAsync(guildChannel.Guild, userMessage);
        }
    }
}
