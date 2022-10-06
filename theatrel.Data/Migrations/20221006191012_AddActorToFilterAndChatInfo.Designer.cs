﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using theatrel.DataAccess;

#nullable disable

namespace theatrel.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20221006191012_AddActorToFilterAndChatInfo")]
    partial class AddActorToFilterAndChatInfo
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.ActorEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Url")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Actors");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.ActorInRoleEntity", b =>
                {
                    b.Property<int>("ActorId")
                        .HasColumnType("integer");

                    b.Property<int>("RoleId")
                        .HasColumnType("integer");

                    b.Property<int>("PlaybillId")
                        .HasColumnType("integer");

                    b.HasKey("ActorId", "RoleId", "PlaybillId");

                    b.HasIndex("PlaybillId");

                    b.HasIndex("RoleId");

                    b.ToTable("ActorInRole");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.ChatInfoEntity", b =>
                {
                    b.Property<long>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("UserId"));

                    b.Property<string>("Actor")
                        .HasColumnType("text");

                    b.Property<int>("CommandLine")
                        .HasColumnType("integer");

                    b.Property<string>("Culture")
                        .HasColumnType("text");

                    b.Property<int>("CurrentStepId")
                        .HasColumnType("integer");

                    b.Property<string>("DbDays")
                        .HasColumnType("text");

                    b.Property<string>("DbLocations")
                        .HasColumnType("text");

                    b.Property<string>("DbTheatres")
                        .HasColumnType("text");

                    b.Property<string>("DbTypes")
                        .HasColumnType("text");

                    b.Property<int>("DialogState")
                        .HasColumnType("integer");

                    b.Property<string>("Info")
                        .HasColumnType("text");

                    b.Property<DateTime>("LastMessage")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("PerformanceName")
                        .HasColumnType("text");

                    b.Property<int>("PreviousStepId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("When")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("UserId");

                    b.ToTable("TlChats");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.LocationsEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int?>("TheatreId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("TheatreId");

                    b.ToTable("PerformanceLocations");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PerformanceEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("LocationId")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("TypeId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("LocationId");

                    b.HasIndex("TypeId");

                    b.ToTable("Performances");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PerformanceFilterEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Actor")
                        .HasColumnType("text");

                    b.Property<string>("DbDaysOfWeek")
                        .HasColumnType("text");

                    b.Property<string>("DbLocations")
                        .HasColumnType("text");

                    b.Property<string>("DbPerformanceTypes")
                        .HasColumnType("text");

                    b.Property<string>("DbTheatres")
                        .HasColumnType("text");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PartOfDay")
                        .HasColumnType("integer");

                    b.Property<string>("PerformanceName")
                        .HasColumnType("text");

                    b.Property<int>("PlaybillId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("Filters");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PerformanceTypeEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("TypeName")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("PerformanceTypes");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PlaybillChangeEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("MinPrice")
                        .HasColumnType("integer");

                    b.Property<int>("PlaybillEntityId")
                        .HasColumnType("integer");

                    b.Property<int>("ReasonOfChanges")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("PlaybillEntityId");

                    b.ToTable("PlaybillChanges");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PlaybillEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<int>("PerformanceId")
                        .HasColumnType("integer");

                    b.Property<string>("TicketsUrl")
                        .HasColumnType("text");

                    b.Property<string>("Url")
                        .HasColumnType("text");

                    b.Property<DateTime>("When")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("PerformanceId");

                    b.ToTable("Playbill");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.RoleEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("CharacterName")
                        .HasColumnType("text");

                    b.Property<int>("PerformanceId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("PerformanceId");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.SubscriptionEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AutoProlongation")
                        .HasColumnType("integer");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PerformanceFilterId")
                        .HasColumnType("integer");

                    b.Property<long>("TelegramUserId")
                        .HasColumnType("bigint");

                    b.Property<int>("TrackingChanges")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("PerformanceFilterId");

                    b.HasIndex("TelegramUserId");

                    b.ToTable("Subscriptions");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.TelegramUserEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Culture")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("TlUsers");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.TheatreEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Theatre");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.ActorInRoleEntity", b =>
                {
                    b.HasOne("theatrel.DataAccess.Structures.Entities.ActorEntity", "Actor")
                        .WithMany("ActorInRole")
                        .HasForeignKey("ActorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("theatrel.DataAccess.Structures.Entities.PlaybillEntity", "Playbill")
                        .WithMany("Cast")
                        .HasForeignKey("PlaybillId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("theatrel.DataAccess.Structures.Entities.RoleEntity", "Role")
                        .WithMany("ActorInRole")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Actor");

                    b.Navigation("Playbill");

                    b.Navigation("Role");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.LocationsEntity", b =>
                {
                    b.HasOne("theatrel.DataAccess.Structures.Entities.TheatreEntity", "Theatre")
                        .WithMany()
                        .HasForeignKey("TheatreId");

                    b.Navigation("Theatre");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PerformanceEntity", b =>
                {
                    b.HasOne("theatrel.DataAccess.Structures.Entities.LocationsEntity", "Location")
                        .WithMany()
                        .HasForeignKey("LocationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("theatrel.DataAccess.Structures.Entities.PerformanceTypeEntity", "Type")
                        .WithMany()
                        .HasForeignKey("TypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Location");

                    b.Navigation("Type");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PlaybillChangeEntity", b =>
                {
                    b.HasOne("theatrel.DataAccess.Structures.Entities.PlaybillEntity", "PlaybillEntity")
                        .WithMany("Changes")
                        .HasForeignKey("PlaybillEntityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PlaybillEntity");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PlaybillEntity", b =>
                {
                    b.HasOne("theatrel.DataAccess.Structures.Entities.PerformanceEntity", "Performance")
                        .WithMany()
                        .HasForeignKey("PerformanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Performance");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.RoleEntity", b =>
                {
                    b.HasOne("theatrel.DataAccess.Structures.Entities.PerformanceEntity", "Performance")
                        .WithMany()
                        .HasForeignKey("PerformanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Performance");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.SubscriptionEntity", b =>
                {
                    b.HasOne("theatrel.DataAccess.Structures.Entities.PerformanceFilterEntity", "PerformanceFilter")
                        .WithMany()
                        .HasForeignKey("PerformanceFilterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("theatrel.DataAccess.Structures.Entities.TelegramUserEntity", "TelegramUser")
                        .WithMany()
                        .HasForeignKey("TelegramUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PerformanceFilter");

                    b.Navigation("TelegramUser");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.ActorEntity", b =>
                {
                    b.Navigation("ActorInRole");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PlaybillEntity", b =>
                {
                    b.Navigation("Cast");

                    b.Navigation("Changes");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.RoleEntity", b =>
                {
                    b.Navigation("ActorInRole");
                });
#pragma warning restore 612, 618
        }
    }
}
