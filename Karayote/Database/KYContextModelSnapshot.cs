﻿// <auto-generated />
using System;
using Karayote.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Karayote.Database
{
    [DbContext(typeof(KYContext))]
    partial class KYContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Karayote.Models.KarayoteUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Karayote.Models.SelectedSong", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("SongQueueId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("SungTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("SongQueueId");

                    b.HasIndex("UserId");

                    b.ToTable("Songs");

                    b.HasDiscriminator<string>("Discriminator").HasValue("SelectedSong");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("Karayote.Models.Session", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime?>("EndTime")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("OpenTime")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("StartTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Sessions");
                });

            modelBuilder.Entity("Karayote.Models.SongQueue", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("SessionId")
                        .HasColumnType("int");

                    b.Property<string>("TextVersion")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("SessionId")
                        .IsUnique();

                    b.ToTable("SongQueues");
                });

            modelBuilder.Entity("Karayote.Models.KarafunSong", b =>
                {
                    b.HasBaseType("Karayote.Models.SelectedSong");

                    b.HasDiscriminator().HasValue("KarafunSong");
                });

            modelBuilder.Entity("Karayote.Models.PlaceholderSong", b =>
                {
                    b.HasBaseType("Karayote.Models.SelectedSong");

                    b.HasDiscriminator().HasValue("PlaceholderSong");
                });

            modelBuilder.Entity("Karayote.Models.YoutubeSong", b =>
                {
                    b.HasBaseType("Karayote.Models.SelectedSong");

                    b.HasDiscriminator().HasValue("YoutubeSong");
                });

            modelBuilder.Entity("Karayote.Models.SelectedSong", b =>
                {
                    b.HasOne("Karayote.Models.SongQueue", null)
                        .WithMany("TheQueue")
                        .HasForeignKey("SongQueueId");

                    b.HasOne("Karayote.Models.KarayoteUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Karayote.Models.SongQueue", b =>
                {
                    b.HasOne("Karayote.Models.Session", "Session")
                        .WithOne("SongQueue")
                        .HasForeignKey("Karayote.Models.SongQueue", "SessionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Session");
                });

            modelBuilder.Entity("Karayote.Models.Session", b =>
                {
                    b.Navigation("SongQueue")
                        .IsRequired();
                });

            modelBuilder.Entity("Karayote.Models.SongQueue", b =>
                {
                    b.Navigation("TheQueue");
                });
#pragma warning restore 612, 618
        }
    }
}
