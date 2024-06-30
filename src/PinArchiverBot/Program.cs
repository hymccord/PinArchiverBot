using Discord.Addons.Hosting;
using Discord.WebSocket;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using PinArchiverBot.Persistence;

namespace PinArchiverBot;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        string? discordToken = builder.Configuration["DiscordBotSettings:Token"];

        if (string.IsNullOrWhiteSpace(discordToken))
        {
            Console.WriteLine("Error: No discord token found. Please provide a token via the DiscordBotSettings:Token environment variable.");
            //Environment.Exit(1);
        }

        // ⚙ Configuration
        builder.Services.AddOptions<DiscordBotSettings>().BindConfiguration(nameof(DiscordBotSettings));

        // 🛠 Services
        builder.Services.AddDbContextFactory<PinArchiverDbContext>(o =>
        {
            o.UseSqlite(builder.Configuration.GetConnectionString("PinArchiverDbContext"));
        });

        builder.Services.AddDiscordHost((config, services) =>
        {
            config.SocketConfig = new DiscordSocketConfig
            {
                LogLevel = Discord.LogSeverity.Verbose,
                AlwaysDownloadUsers = false,
                MessageCacheSize = 50,
                GatewayIntents = Discord.GatewayIntents.GuildMessages | Discord.GatewayIntents.Guilds | Discord.GatewayIntents.MessageContent
            };

            config.Token = services.GetRequiredService<IOptions<DiscordBotSettings>>().Value.Token;
        });

        builder.Services.AddInteractionService((config, _) =>
        {
            config.LogLevel = Discord.LogSeverity.Info;
            config.UseCompiledLambda = true;
        });

        builder.Services.AddSingleton<PinArchiverService>();
        builder.Services.AddSingleton<IPinArchiverService>(provider => provider.GetRequiredService<PinArchiverService>());

        // 🚀 Hosted Services
        builder.Services.AddHostedService<DatabaseMigrationService>();
        builder.Services.AddHostedService<InteractionHandlerService>();
        builder.Services.AddHostedService<BotStatusService>();
        builder.Services.AddHostedService<MessageHandlerService>();

        await builder.Build().RunAsync();
    }
}

public record DiscordBotSettings
{
    public string Token { get; init; } = string.Empty;
    public ulong DebugGuildId { get; init; }
}