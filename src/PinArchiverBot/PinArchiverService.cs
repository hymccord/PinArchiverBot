using DSharpPlus;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands;
using DSharpPlus.Entities;

using Microsoft.Extensions.Hosting;

using PinArchiverBot;
using Microsoft.Extensions.Options;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;

internal class PinArchiverService : IHostedService
{
    private readonly DiscordClient _discordClient;
    private readonly IOptions<DiscordBotSettings> _botSettings;

    public PinArchiverService(DiscordClient discordClient, IOptions<DiscordBotSettings> botSettings)
    {
        _discordClient = discordClient;
        _botSettings = botSettings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Register extensions outside of the service provider lambda since these involve asynchronous operations
        CommandsExtension commandsExtension = _discordClient.UseCommands(new CommandsConfiguration()
        {
            DebugGuildId = _botSettings.Value.DebugGuildId,
            // The default value, however it's shown here for clarity
            RegisterDefaultCommandProcessors = true
        });

        // Add all commands by scanning the current assembly
        commandsExtension.AddCommands(typeof(Program).Assembly);
        TextCommandProcessor textCommandProcessor = new(new()
        {
            // The default behavior is that the bot reacts to direct mentions
            // and to the "!" prefix.
            // If you want to change it, you first set if the bot should react to mentions
            // and then you can provide as many prefixes as you want.
            PrefixResolver = new DefaultPrefixResolver(true, "?", "&").ResolvePrefixAsync
        });

        //// Add text commands with a custom prefix (?ping)
        await commandsExtension.AddProcessorsAsync(textCommandProcessor);

        DiscordActivity status = new("pins", DiscordActivityType.Watching);

        await _discordClient.ConnectAsync(status, DiscordUserStatus.Online);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discordClient.DisconnectAsync();
    }
}