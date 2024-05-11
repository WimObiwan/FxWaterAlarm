﻿// <auto-generated />
using System;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Core.Migrations
{
    [DbContext(typeof(WaterAlarmDbContext))]
    [Migration("20240511164613_AddAccountSensorAlarms")]
    partial class AddAccountSensorAlarms
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.3");

            modelBuilder.Entity("Core.Entities.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreationTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Link")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Uid")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("Link")
                        .IsUnique();

                    b.HasIndex("Uid")
                        .IsUnique();

                    b.ToTable("Account", (string)null);
                });

            modelBuilder.Entity("Core.Entities.AccountSensor", b =>
                {
                    b.Property<int>("AccountId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SensorId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AlertsEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("CapacityL")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreateTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<int?>("DistanceMmEmpty")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DistanceMmFull")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("AccountId", "SensorId");

                    b.HasIndex("SensorId");

                    b.ToTable("AccountSensor", (string)null);
                });

            modelBuilder.Entity("Core.Entities.AccountSensorAlarm", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AccountSensorAccountId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AccountSensorSensorId")
                        .HasColumnType("INTEGER");

                    b.Property<double?>("AlarmThreshold")
                        .HasColumnType("REAL");

                    b.Property<int>("AlarmType")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("Uid")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Uid")
                        .IsUnique();

                    b.HasIndex("AccountSensorAccountId", "AccountSensorSensorId");

                    b.ToTable("AccountSensorAlarm", (string)null);
                });

            modelBuilder.Entity("Core.Entities.Sensor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreateTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<string>("DevEui")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Link")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Uid")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Link")
                        .IsUnique();

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

            modelBuilder.Entity("Core.Entities.AccountSensorAlarm", b =>
                {
                    b.HasOne("Core.Entities.AccountSensor", "AccountSensor")
                        .WithMany("Alarms")
                        .HasForeignKey("AccountSensorAccountId", "AccountSensorSensorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AccountSensor");
                });

            modelBuilder.Entity("Core.Entities.Account", b =>
                {
                    b.Navigation("AccountSensors");
                });

            modelBuilder.Entity("Core.Entities.AccountSensor", b =>
                {
                    b.Navigation("Alarms");
                });

            modelBuilder.Entity("Core.Entities.Sensor", b =>
                {
                    b.Navigation("AccountSensors");
                });
#pragma warning restore 612, 618
        }
    }
}
