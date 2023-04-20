﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PMTS.Models;

#nullable disable

namespace PMTS.Migrations
{
    [DbContext(typeof(PSQLcontext))]
    partial class PSQLcontextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("PMTS.Models.Bird", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Points")
                        .HasColumnType("integer");

                    b.Property<int>("TournamentId")
                        .HasColumnType("integer");

                    b.Property<string>("TournamentName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("TournamentId");

                    b.ToTable("Bird");
                });

            modelBuilder.Entity("PMTS.Models.Contestant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Points")
                        .HasColumnType("integer");

                    b.Property<bool>("Removed")
                        .HasColumnType("boolean");

                    b.Property<int>("TournamentId")
                        .HasColumnType("integer");

                    b.Property<string>("TournamentName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("TournamentId");

                    b.ToTable("Contestant");
                });

            modelBuilder.Entity("PMTS.Models.Helper", b =>
                {
                    b.Property<string>("crypt")
                        .IsRequired()
                        .HasColumnType("text");

                    b.ToTable("Helper");
                });

            modelBuilder.Entity("PMTS.Models.Photo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("BirdDetected")
                        .HasColumnType("boolean");

                    b.Property<int>("BirdN")
                        .HasColumnType("integer");

                    b.Property<int>("ContestantId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Points")
                        .HasColumnType("integer");

                    b.Property<int>("TournamentId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ContestantId");

                    b.ToTable("Photo");
                });

            modelBuilder.Entity("PMTS.Models.Tournament", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("Active")
                        .HasColumnType("boolean");

                    b.Property<int>("DefaultPoints")
                        .HasColumnType("integer");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsPrivate")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("Ongoing")
                        .HasColumnType("boolean");

                    b.Property<string>("Organizer")
                        .HasColumnType("text");

                    b.Property<bool>("RestrictedTypes")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Tournament");
                });

            modelBuilder.Entity("PMTS.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("Admin")
                        .HasColumnType("boolean");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(320)
                        .HasColumnType("character varying(320)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(72)
                        .HasColumnType("character varying(72)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("PMTS.Models.Bird", b =>
                {
                    b.HasOne("PMTS.Models.Tournament", null)
                        .WithMany("Birds")
                        .HasForeignKey("TournamentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PMTS.Models.Contestant", b =>
                {
                    b.HasOne("PMTS.Models.Tournament", null)
                        .WithMany("Contestants")
                        .HasForeignKey("TournamentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PMTS.Models.Photo", b =>
                {
                    b.HasOne("PMTS.Models.Contestant", null)
                        .WithMany("Photos")
                        .HasForeignKey("ContestantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PMTS.Models.Tournament", b =>
                {
                    b.HasOne("PMTS.Models.User", null)
                        .WithMany("Tournaments")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("PMTS.Models.Contestant", b =>
                {
                    b.Navigation("Photos");
                });

            modelBuilder.Entity("PMTS.Models.Tournament", b =>
                {
                    b.Navigation("Birds");

                    b.Navigation("Contestants");
                });

            modelBuilder.Entity("PMTS.Models.User", b =>
                {
                    b.Navigation("Tournaments");
                });
#pragma warning restore 612, 618
        }
    }
}
