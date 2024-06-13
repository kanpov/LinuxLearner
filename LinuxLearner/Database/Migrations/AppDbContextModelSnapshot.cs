﻿// <auto-generated />
using System;
using LinuxLearner.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LinuxLearner.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("LinuxLearner.Domain.Course", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("AcceptanceMode")
                        .HasColumnType("integer");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Courses", (string)null);
                });

            modelBuilder.Entity("LinuxLearner.Domain.CourseInvite", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CourseId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("ExpirationTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("UsageAmount")
                        .HasColumnType("integer");

                    b.Property<int>("UsageLimit")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.ToTable("CourseInvites", (string)null);
                });

            modelBuilder.Entity("LinuxLearner.Domain.CourseParticipation", b =>
                {
                    b.Property<Guid>("CourseId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsCourseAdministrator")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset>("JoinTime")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("CourseId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("CourseParticipations", (string)null);
                });

            modelBuilder.Entity("LinuxLearner.Domain.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("UserType")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Users", (string)null);
                });

            modelBuilder.Entity("LinuxLearner.Domain.CourseInvite", b =>
                {
                    b.HasOne("LinuxLearner.Domain.Course", "Course")
                        .WithMany()
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Course");
                });

            modelBuilder.Entity("LinuxLearner.Domain.CourseParticipation", b =>
                {
                    b.HasOne("LinuxLearner.Domain.Course", "Course")
                        .WithMany("CourseParticipations")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("LinuxLearner.Domain.User", "User")
                        .WithMany("CourseParticipations")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Course");

                    b.Navigation("User");
                });

            modelBuilder.Entity("LinuxLearner.Domain.Course", b =>
                {
                    b.Navigation("CourseParticipations");
                });

            modelBuilder.Entity("LinuxLearner.Domain.User", b =>
                {
                    b.Navigation("CourseParticipations");
                });
#pragma warning restore 612, 618
        }
    }
}
