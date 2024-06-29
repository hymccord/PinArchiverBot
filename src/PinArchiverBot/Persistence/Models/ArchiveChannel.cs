using System.ComponentModel.DataAnnotations;

namespace PinArchiverBot.Persistence.Models;
internal class ArchiveChannel
{
    [Key]
    public ulong GuildId { get; set; }

    public ulong Channel { get; set; }
}
