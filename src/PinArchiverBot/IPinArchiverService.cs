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
    Task BlacklistChannelAsync(ulong guildId, ulong channelId);
    Task WhitelistChannelAsync(ulong guildId, ulong channelId);
}

class PinArchiverService : IPinArchiverService
{
    private readonly ILogger<PinArchiverService> _logger;
    private readonly IDbContextFactory<PinArchiverDbContext> _contextFactory;
    private readonly DiscordSocketClient _client;

    private ConcurrentDictionary<ulong, ulong> _guildArchiveChannelCache = [];
    // This is just a ConcurrentHashSet, channelIDs are unique
    private ConcurrentDictionary<ulong, ulong> _blacklistedChannels = [];

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
        var blacklistChannels = await context.BlacklistChannels
            .Select(ac => new KeyValuePair<ulong, ulong>(ac.ChannelId, ac.ChannelId))
            .ToListAsync();
        _blacklistedChannels = new(blacklistChannels);

        _ = Task.Run(ProcessMessagesAsync);
    }

    public async Task EnableArchiveChannelAsync(ulong guildId, ulong channelId)
    {
        await DisableArchiveChannelAsync(guildId);

        if (!_guildArchiveChannelCache.TryAdd(guildId, channelId))
        {
            return;
        }

        using var context = _contextFactory.CreateDbContext();
        await context.ArchiveChannels.AddAsync(new ArchiveChannel
        {
            GuildId = guildId,
            ChannelId = channelId
        });
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DisableArchiveChannelAsync(ulong guildId)
    {
        if (!_guildArchiveChannelCache.TryRemove(guildId, out ulong channelId))
        {
            return;
        }

        using var context = _contextFactory.CreateDbContext();
        ArchiveChannel? channel = await context.ArchiveChannels.FindAsync(guildId);

        if (channel is not null)
        {
            context.ArchiveChannels.Remove(channel);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task BlacklistChannelAsync(ulong guildId, ulong channelId)
    {
        if (_blacklistedChannels.ContainsKey(channelId))
        {
            return;
        }

        _logger.LogDebug("Blacklisting channel {ChannelId}", channelId);

        _blacklistedChannels.TryAdd(channelId, channelId);
        var context = _contextFactory.CreateDbContext();
        await context.BlacklistChannels.AddAsync(new BlacklistChannel
        {
            GuildId = guildId,
            ChannelId = channelId
        });
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task WhitelistChannelAsync(ulong guildId, ulong channelId)
    {
        if (!_blacklistedChannels.ContainsKey(channelId))
        {
            return;
        }

        _logger.LogDebug("Whitelisting channel {ChannelId}", channelId);

        _blacklistedChannels.TryRemove(channelId, out _);
        var context = _contextFactory.CreateDbContext();
        BlacklistChannel? channel = await context.BlacklistChannels
            .Where(bc => bc.ChannelId == channelId)
            .SingleOrDefaultAsync();
        if (channel is not null) {
            context.BlacklistChannels.Remove(channel);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task OnMessageEditedAsync(IGuild guild, IUserMessage message)
    {
        if (!message.IsPinned)
        {
            return;
        }

        if (_blacklistedChannels.ContainsKey(message.Channel.Id))
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
