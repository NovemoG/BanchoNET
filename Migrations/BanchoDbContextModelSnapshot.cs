﻿// <auto-generated />
using System;
using BanchoNET.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BanchoNET.Migrations
{
    [DbContext(typeof(BanchoDbContext))]
    partial class BanchoDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("BanchoNET.Models.Dtos.BeatmapDto", b =>
                {
                    b.Property<int>("MapId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<float>("Ar")
                        .HasColumnType("FLOAT(4,2)");

                    b.Property<string>("Artist")
                        .IsRequired()
                        .HasMaxLength(128)
                        .IsUnicode(false)
                        .HasColumnType("varchar(128)");

                    b.Property<float>("Bpm")
                        .HasColumnType("FLOAT(15,3)");

                    b.Property<string>("Creator")
                        .IsRequired()
                        .HasMaxLength(16)
                        .IsUnicode(false)
                        .HasColumnType("varchar(16)");

                    b.Property<float>("Cs")
                        .HasColumnType("FLOAT(4,2)");

                    b.Property<bool>("Frozen")
                        .HasColumnType("tinyint(1)");

                    b.Property<float>("Hp")
                        .HasColumnType("FLOAT(4,2)");

                    b.Property<bool>("IsRankedOfficially")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("MD5")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("CHAR(32)");

                    b.Property<int>("MaxCombo")
                        .HasColumnType("int");

                    b.Property<byte>("Mode")
                        .HasColumnType("tinyint unsigned");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(128)
                        .IsUnicode(false)
                        .HasColumnType("varchar(128)");

                    b.Property<int>("NotesCount")
                        .HasColumnType("int");

                    b.Property<float>("Od")
                        .HasColumnType("FLOAT(4,2)");

                    b.Property<long>("Passes")
                        .HasColumnType("bigint");

                    b.Property<long>("Plays")
                        .HasColumnType("bigint");

                    b.Property<bool>("Private")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("SetId")
                        .HasColumnType("int");

                    b.Property<int>("SlidersCount")
                        .HasColumnType("int");

                    b.Property<int>("SpinnersCount")
                        .HasColumnType("int");

                    b.Property<float>("StarRating")
                        .HasColumnType("FLOAT(9,3)");

                    b.Property<sbyte>("Status")
                        .HasColumnType("tinyint");

                    b.Property<DateTime>("SubmitDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(128)
                        .IsUnicode(false)
                        .HasColumnType("varchar(128)");

                    b.Property<int>("TotalLength")
                        .HasColumnType("int");

                    b.HasKey("MapId");

                    b.HasIndex("MD5")
                        .IsUnique();

                    b.ToTable("Beatmaps");
                });

            modelBuilder.Entity("BanchoNET.Models.Dtos.ClientHashesDto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Adapters")
                        .IsRequired()
                        .HasMaxLength(32)
                        .IsUnicode(false)
                        .HasColumnType("CHAR");

                    b.Property<string>("DiskSerial")
                        .IsRequired()
                        .HasMaxLength(32)
                        .IsUnicode(false)
                        .HasColumnType("CHAR");

                    b.Property<DateTime>("LatestTime")
                        .HasColumnType("DATETIME");

                    b.Property<string>("OsuPath")
                        .IsRequired()
                        .HasMaxLength(32)
                        .IsUnicode(false)
                        .HasColumnType("CHAR");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<string>("Uninstall")
                        .IsRequired()
                        .HasMaxLength(32)
                        .IsUnicode(false)
                        .HasColumnType("CHAR");

                    b.HasKey("Id");

                    b.ToTable("ClientHashes");
                });

            modelBuilder.Entity("BanchoNET.Models.Dtos.LoginDto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Ip")
                        .IsRequired()
                        .HasMaxLength(45)
                        .IsUnicode(false)
                        .HasColumnType("varchar(45)");

                    b.Property<DateTime>("LoginTime")
                        .HasColumnType("DATETIME");

                    b.Property<DateTime>("OsuVersion")
                        .HasColumnType("DATETIME");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<string>("ReleaseStream")
                        .IsRequired()
                        .HasMaxLength(11)
                        .IsUnicode(false)
                        .HasColumnType("varchar(11)");

                    b.HasKey("Id");

                    b.ToTable("PlayerLogins");
                });

            modelBuilder.Entity("BanchoNET.Models.Dtos.PlayerDto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:IdentityIncrement", 1)
                        .HasAnnotation("SqlServer:IdentitySeed", 3L)
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ApiKey")
                        .HasMaxLength(36)
                        .IsUnicode(false)
                        .HasColumnType("CHAR");

                    b.Property<string>("AwayMessage")
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<string>("Country")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("CHAR(2)");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("DATETIME");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(160)
                        .IsUnicode(false)
                        .HasColumnType("varchar(160)");

                    b.Property<DateTime>("LastActivityTime")
                        .HasColumnType("DATETIME");

                    b.Property<string>("LoginName")
                        .IsRequired()
                        .HasMaxLength(16)
                        .IsUnicode(false)
                        .HasColumnType("varchar(16)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("CHAR(60)");

                    b.Property<byte>("PlayStyle")
                        .HasColumnType("tinyint unsigned");

                    b.Property<sbyte>("PreferredMode")
                        .HasColumnType("TINYINT(2)");

                    b.Property<int>("Privileges")
                        .HasColumnType("int");

                    b.Property<int>("RemainingSilence")
                        .HasColumnType("int");

                    b.Property<int>("RemainingSupporter")
                        .HasColumnType("int");

                    b.Property<string>("SafeName")
                        .IsRequired()
                        .HasMaxLength(16)
                        .IsUnicode(false)
                        .HasColumnType("varchar(16)");

                    b.Property<string>("UserPageContent")
                        .HasMaxLength(4096)
                        .HasColumnType("varchar(4096)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(16)
                        .IsUnicode(false)
                        .HasColumnType("varchar(16)");

                    b.HasKey("Id");

                    b.HasIndex("ApiKey")
                        .IsUnique();

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("LoginName")
                        .IsUnique();

                    b.HasIndex("SafeName")
                        .IsUnique();

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Players");
                });

            modelBuilder.Entity("BanchoNET.Models.Dtos.RelationshipDto", b =>
                {
                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<byte>("Relation")
                        .HasColumnType("tinyint unsigned");

                    b.Property<int>("TargetId")
                        .HasColumnType("int");

                    b.HasKey("PlayerId");

                    b.ToTable("Relationships");
                });

            modelBuilder.Entity("BanchoNET.Models.Dtos.ScoreDto", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<float>("Acc")
                        .HasColumnType("FLOAT(6,3)");

                    b.Property<string>("BeatmapMD5")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("CHAR(32)");

                    b.Property<int>("ClientFlags")
                        .HasColumnType("int");

                    b.Property<int>("Count100")
                        .HasColumnType("int");

                    b.Property<int>("Count300")
                        .HasColumnType("int");

                    b.Property<int>("Count50")
                        .HasColumnType("int");

                    b.Property<int>("Gekis")
                        .HasColumnType("int");

                    b.Property<sbyte>("Grade")
                        .HasColumnType("TINYINT(2)");

                    b.Property<int>("Katus")
                        .HasColumnType("int");

                    b.Property<int>("MaxCombo")
                        .HasColumnType("int");

                    b.Property<int>("Misses")
                        .HasColumnType("int");

                    b.Property<sbyte>("Mode")
                        .HasColumnType("TINYINT(2)");

                    b.Property<int>("Mods")
                        .HasColumnType("int");

                    b.Property<string>("OnlineChecksum")
                        .IsRequired()
                        .IsUnicode(false)
                        .HasColumnType("CHAR(32)");

                    b.Property<float>("PP")
                        .HasColumnType("FLOAT(7,3)");

                    b.Property<bool>("Perfect")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("PlayTime")
                        .HasColumnType("DATETIME");

                    b.Property<int>("Score")
                        .HasColumnType("int");

                    b.Property<sbyte>("Status")
                        .HasColumnType("TINYINT(2)");

                    b.Property<int>("TimeElapsed")
                        .HasColumnType("int");

                    b.HasKey("Id", "PlayerId");

                    b.ToTable("Scores");
                });

            modelBuilder.Entity("BanchoNET.Models.Dtos.StatsDto", b =>
                {
                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<byte>("Mode")
                        .HasColumnType("tinyint unsigned");

                    b.Property<int>("ACount")
                        .HasColumnType("int");

                    b.Property<float>("Accuracy")
                        .HasColumnType("FLOAT(6,3)");

                    b.Property<int>("MaxCombo")
                        .HasColumnType("int");

                    b.Property<ushort>("PP")
                        .HasColumnType("smallint unsigned");

                    b.Property<int>("PlayCount")
                        .HasColumnType("int");

                    b.Property<int>("PlayTime")
                        .HasColumnType("int");

                    b.Property<long>("RankedScore")
                        .HasColumnType("bigint");

                    b.Property<int>("ReplayViews")
                        .HasColumnType("int");

                    b.Property<int>("SCount")
                        .HasColumnType("int");

                    b.Property<int>("SHCount")
                        .HasColumnType("int");

                    b.Property<int>("Total100s")
                        .HasColumnType("int");

                    b.Property<int>("Total300s")
                        .HasColumnType("int");

                    b.Property<int>("Total50s")
                        .HasColumnType("int");

                    b.Property<int>("TotalGekis")
                        .HasColumnType("int");

                    b.Property<int>("TotalKatus")
                        .HasColumnType("int");

                    b.Property<long>("TotalScore")
                        .HasColumnType("bigint");

                    b.Property<int>("XCount")
                        .HasColumnType("int");

                    b.Property<int>("XHCount")
                        .HasColumnType("int");

                    b.HasKey("PlayerId", "Mode");

                    b.ToTable("Stats");
                });
#pragma warning restore 612, 618
        }
    }
}
