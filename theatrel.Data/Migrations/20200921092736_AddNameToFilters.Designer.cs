﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using theatrel.DataAccess;

namespace theatrel.DataAccess.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20200921092736_AddNameToFilters")]
    partial class AddNameToFilters
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.ChatInfoEntity", b =>
                {
                    b.Property<long>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("CommandLine")
                        .HasColumnType("integer");

                    b.Property<string>("Culture")
                        .HasColumnType("text");

                    b.Property<int>("CurrentStepId")
                        .HasColumnType("integer");

                    b.Property<string>("DbDays")
                        .HasColumnType("text");

                    b.Property<string>("DbTypes")
                        .HasColumnType("text");

                    b.Property<int>("DialogState")
                        .HasColumnType("integer");

                    b.Property<DateTime>("LastMessage")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("PerformanceName")
                        .HasColumnType("text");

                    b.Property<int>("PreviousStepId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("When")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("UserId");

                    b.ToTable("TlChats");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.LocationsEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("PerformanceLocations");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PerformanceEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("DbDaysOfWeek")
                        .HasColumnType("text");

                    b.Property<string>("DbLocations")
                        .HasColumnType("text");

                    b.Property<string>("DbPerformanceTypes")
                        .HasColumnType("text");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("PartOfDay")
                        .HasColumnType("integer");

                    b.Property<string>("PerformanceName")
                        .HasColumnType("text");

                    b.Property<int>("PlaybillId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.ToTable("Filters");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PerformanceTypeEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("TypeName")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("PerformanceTypes");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PlaybillChangeEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp without time zone");

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
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("PerformanceId")
                        .HasColumnType("integer");

                    b.Property<string>("Url")
                        .HasColumnType("text");

                    b.Property<DateTime>("When")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("PerformanceId");

                    b.ToTable("Playbill");
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.SubscriptionEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp without time zone");

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
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Culture")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("TlUsers");
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
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PlaybillChangeEntity", b =>
                {
                    b.HasOne("theatrel.DataAccess.Structures.Entities.PlaybillEntity", "PlaybillEntity")
                        .WithMany("Changes")
                        .HasForeignKey("PlaybillEntityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("theatrel.DataAccess.Structures.Entities.PlaybillEntity", b =>
                {
                    b.HasOne("theatrel.DataAccess.Structures.Entities.PerformanceEntity", "Performance")
                        .WithMany()
                        .HasForeignKey("PerformanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
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
                });
#pragma warning restore 612, 618
        }
    }
}
