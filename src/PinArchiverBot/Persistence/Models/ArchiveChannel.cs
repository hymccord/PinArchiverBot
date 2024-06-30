﻿using System.ComponentModel.DataAnnotations;

namespace PinArchiverBot.Persistence.Models;
public class ArchiveChannel
{
    [Key]
    public ulong GuildId { get; set; }

    public ulong ChannelId { get; set; }
}
