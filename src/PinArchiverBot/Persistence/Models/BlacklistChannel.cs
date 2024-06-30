using System.ComponentModel.DataAnnotations;

namespace PinArchiverBot.Persistence.Models;
public class BlacklistChannel
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}
