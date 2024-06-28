using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.Interactions;
using Discord.WebSocket;

namespace PinArchiverBot.SlashCommands;
public class PingCommand : InteractionModuleBase
{
    [SlashCommand("ping", "Pings the bot and returns its latency.")]
    public async Task GreetUserAsync()
    => await RespondAsync(text: $":ping_pong: It took me {((BaseSocketClient)Context.Client).Latency}ms to respond to you!", ephemeral: true);
}