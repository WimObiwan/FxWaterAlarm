﻿// <auto-generated />
using System;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Core.Migrations
{
    [DbContext(typeof(WaterAlarmDbContext))]
    partial class WaterAlarmDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.3");

            modelBuilder.Entity("Core.Entities.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Uid")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("Uid")
                        .IsUnique();

                    b.ToTable("Accounts", (string)null);
                });

            modelBuilder.Entity("Core.Entities.AccountSensor", b =>
                {
                    b.Property<int>("AccountId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SensorId")
                        .HasColumnType("INTEGER");

                    b.HasKey("AccountId", "SensorId");

                    b.HasIndex("SensorId");

                    b.ToTable("AccountSensor", (string)null);
                });

            modelBuilder.Entity("Core.Entities.Sensor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("DevEui")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Uid")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Uid")
                        .IsUnique();

                    b.ToTable("Sensor", (string)null);
                });

            modelBuilder.Entity("Core.Entities.AccountSensor", b =>
                {
                    b.HasOne("Core.Entities.Account", "Account")
                        .WithMany("AccountSensors")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Core.Entities.Sensor", "Sensor")
                        .WithMany("AccountSensors")
                        .HasForeignKey("SensorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");

                    b.Navigation("Sensor");
                });

            modelBuilder.Entity("Core.Entities.Account", b =>
                {
                    b.Navigation("AccountSensors");
                });

            modelBuilder.Entity("Core.Entities.Sensor", b =>
                {
                    b.Navigation("AccountSensors");
                });
#pragma warning restore 612, 618
        }
    }
}
