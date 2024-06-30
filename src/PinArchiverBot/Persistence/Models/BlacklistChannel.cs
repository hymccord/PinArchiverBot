using System.ComponentModel.DataAnnotations;

namespace PinArchiverBot.Persistence.Models;
public class BlacklistChannel
{
    [Key]
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}
