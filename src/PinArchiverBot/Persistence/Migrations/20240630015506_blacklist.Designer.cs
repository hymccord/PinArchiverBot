﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PinArchiverBot.Persistence;

#nullable disable

namespace PinArchiverBot.Persistence.Migrations
{
    [DbContext(typeof(PinArchiverDbContext))]
    [Migration("20240630015506_blacklist")]
    partial class blacklist
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.6");

            modelBuilder.Entity("PinArchiverBot.Persistence.Models.ArchiveChannel", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.HasKey("GuildId");

                    b.ToTable("ArchiveChannels");
                });

            modelBuilder.Entity("PinArchiverBot.Persistence.Models.BlacklistChannel", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.HasKey("GuildId");

                    b.HasIndex("ChannelId")
                        .IsUnique();

                    b.ToTable("BlacklistChannels");
                });
#pragma warning restore 612, 618
        }
    }
}
