using System.Collections.Concurrent;
using System.Threading.Channels;

using Discord;
using Discord.Net;
using Discord.WebSocket;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using PinArchiverBot.Persistence;
using PinArchiverBot.Persistence.Models;

namespace PinArchiverBot.Services;
public interface IPinArchiverService
{
    /// <summary>
    /// Enable archiving of pinned messages to a channel.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnableArchiveChannelAsync(ulong guildId, ulong channelId);

    /// <summary>
    /// Disables archiving of pinned messages for a specific guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisableArchiveChannelAsync(ulong guildId);

    /// <summary>
    /// Blacklists a channel for archiving pinned messages.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel to be blacklisted.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BlacklistChannelAsync(ulong guildId, ulong channelId);

    /// <summary>
    /// Whitelists a channel for archiving pinned messages.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel to be whitelisted.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WhitelistChannelAsync(ulong guildId, ulong channelId);

    /// <summary>
    /// Archives a channel by sending all pinned messages to the configured archive channel.
    /// </summary>
    /// <param name="channelId">The ID of the channel to be archived.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ArchiveChannelAsync(ulong channelId);
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

    /// <summary>
    /// Initializes the PinArchiverService.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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


    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
        if (channel is null)
        {
            return;
        }

        context.BlacklistChannels.Remove(channel);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task ArchiveChannelAsync(ulong channelId)
    {
        if (await _client.GetChannelAsync(channelId) is not ITextChannel channel)
        {
            return;
        }

        // get all pinned messages in a channel
        var pinnedMessages = await channel.GetPinnedMessagesAsync();
        foreach (var message in pinnedMessages.OfType<IUserMessage>())
        {
            await ArchiveMessageAsync(channel.Guild, message).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles the event when a message is edited in a guild.
    /// If the message is pinned and not in a blacklisted channel, it is added to the message archival queue.
    /// </summary>
    /// <param name="guild">The guild where the message was edited.</param>
    /// <param name="message">The edited message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Processes the messages in the message archival queue and archives them.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Archives a message by sending it to the configured archive channel.
    /// </summary>
    /// <param name="guild">The guild where the message is being archived.</param>
    /// <param name="message">The message to be archived.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ArchiveMessageAsync(IGuild guild, IUserMessage message)
    {
        // If the guild hasn't set up a channel, we can't archive yet.
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
            if (message.Embeds.Count != 0)
            {
                await textChannel.SendMessageAsync(embed: message.Embeds.First().ToEmbedBuilder().Build());
            }
            else
            {
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(message.Author)
                    .WithDescription(message.Content);

                if (message.Attachments.Count != 0)
                {
                    embedBuilder.AddField("Attachments", string.Join("\n", message.Attachments.Select(a => a.Url)));
                }

                embedBuilder
                    .AddField("Original Message", $"[Link]({message.GetJumpUrl()})")
                    .AddField("Author ID", message.Author.Id);

                await textChannel.SendMessageAsync(embed: embedBuilder.Build());
            }
        }
        catch (HttpException ex)
        {
            _logger.LogError(ex, "Failed to send message to archive channel.");
        }
    }
}
