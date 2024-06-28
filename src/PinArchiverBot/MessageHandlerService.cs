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
    public MessageHandlerService(DiscordSocketClient client, ILogger<DiscordClientService> logger)
        : base(client, logger)
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

    private Task HandleUpdatedMessage(Cacheable<IMessage?, ulong> messageBefore, SocketMessage messageAfter, ISocketMessageChannel channel)
    {
        Logger.LogInformation("Updated message: {Message}", messageAfter.Content);
        Logger.LogInformation("Before: {BeforeIsPinned}, After: {AfterIsPinned}", messageBefore.Value?.IsPinned, messageAfter.IsPinned);
        return Task.CompletedTask;
    }
}
