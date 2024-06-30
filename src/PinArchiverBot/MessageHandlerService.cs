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

        Client.MessageReceived += HandleReceivedMessage;
        Client.MessageUpdated += HandleUpdatedMessage;
    }

    private Task HandleReceivedMessage(SocketMessage message)
    {
        Logger.LogInformation("Received message: {Message}", message);

        return Task.CompletedTask;
    }

    private async Task HandleUpdatedMessage(Cacheable<IMessage?, ulong> messageBefore, SocketMessage messageAfter, ISocketMessageChannel channel)
    {
        Logger.LogInformation("Updated message: {Message}", messageAfter.Content);
        Logger.LogInformation("Before: {BeforeIsPinned}, After: {AfterIsPinned}", messageBefore.Value?.IsPinned, messageAfter.IsPinned);

        if (channel is not SocketGuildChannel guildChannel)
        {
            return;
        }

        if (messageAfter is not IUserMessage userMessage)
        {

            return;
        }

        await _archiverService.OnMessageEditedAsync(guildChannel.Guild, userMessage);
    }
}
