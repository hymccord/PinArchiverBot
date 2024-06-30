using System.Collections.Concurrent;
using System.Threading.Channels;

using Discord;
using Discord.Net;
using Discord.WebSocket;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using PinArchiverBot.Persistence;
using PinArchiverBot.Persistence.Models;

namespace PinArchiverBot;
public interface IPinArchiverService
{
    Task EnableArchiveChannelAsync(ulong guildId, ulong channelId);
    Task DisableArchiveChannelAsync(ulong guildId);
}

class PinArchiverService : IPinArchiverService
{
    private readonly ILogger<PinArchiverService> _logger;
    private readonly IDbContextFactory<PinArchiverDbContext> _contextFactory;
    private readonly DiscordSocketClient _client;

    private ConcurrentDictionary<ulong, ulong> _guildArchiveChannelCache = [];

    private readonly Channel<(IGuild guild, IUserMessage message)> _messageArchivalQueue = Channel.CreateUnbounded<(IGuild, IUserMessage)>(new()
    {
        SingleReader = true,
        SingleWriter = false
    });

    public PinArchiverService(ILogger<PinArchiverService> logger, IDbContextFactory<PinArchiverDbContext> contextFactory, DiscordSocketClient client)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _client = client;
    }

    public async Task InitializeAsync()
    {
        using var context = _contextFactory.CreateDbContext();

        var archiveChannels = await context.ArchiveChannels
            .Select(ac => new KeyValuePair<ulong, ulong>(ac.GuildId, ac.ChannelId))
            .ToListAsync();
        _guildArchiveChannelCache = new(archiveChannels);

        _ = Task.Run(ProcessMessagesAsync);
    }

    public async Task EnableArchiveChannelAsync(ulong guildId, ulong channelId)
    {
        using var context = _contextFactory.CreateDbContext();

        await DisableArchiveChannelAsync(guildId);

        if (_guildArchiveChannelCache.TryAdd(guildId, channelId))
        {
            await context.ArchiveChannels.AddAsync(new ArchiveChannel
            {
                GuildId = guildId,
                ChannelId = channelId
            });
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task DisableArchiveChannelAsync(ulong guildId)
    {
        using var context = _contextFactory.CreateDbContext();

        if (_guildArchiveChannelCache.TryRemove(guildId, out ulong channelId))
        {
            ArchiveChannel? channel = await context.ArchiveChannels.FindAsync(guildId, channelId);

            if (channel is not null)
            {
                context.ArchiveChannels.Remove(channel);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task OnMessageEditedAsync(IGuild guild, IUserMessage message)
    {
        if (!message.IsPinned)
        {
            return;
        }

        await _messageArchivalQueue.Writer.WriteAsync((guild, message)).ConfigureAwait(false);
    }

    private async Task ProcessMessagesAsync()
    {
        while (await _messageArchivalQueue.Reader.WaitToReadAsync())
        {

            await foreach (var guildMessage in _messageArchivalQueue.Reader.ReadAllAsync())
            {
               await ArchiveMessageAsync(guildMessage.guild, guildMessage.message).ConfigureAwait(false);
            }

            await Task.Delay(1000);
        }
    }

    private async Task ArchiveMessageAsync(IGuild guild, IUserMessage message)
    {

        if (!_guildArchiveChannelCache.TryGetValue(guild.Id, out ulong archiveChannelId))
        {
            return;
        }

        _logger.LogInformation("Archiving message {MessageId} from author {Author}", message.Id, message.Author.Id);
        var archiveChannel = await _client.GetChannelAsync(archiveChannelId);
        if (archiveChannel is not ITextChannel textChannel)
        {
            return;
        }

        try
        {
            await textChannel.SendMessageAsync(message.Content);

        } catch (HttpException ex)
        {
            _logger.LogError(ex, "Failed to send message to archive channel.");
        }
    }
}
