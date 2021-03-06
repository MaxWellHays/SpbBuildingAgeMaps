﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using SpbBuildingAgeMaps.DataModel;
using System;

namespace SpbBuildingAgeMaps.Migrations
{
    [DbContext(typeof(BuildingContext))]
    partial class BuildingContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.2-rtm-10011");

            modelBuilder.Entity("SpbBuildingAgeMaps.DataModel.BuildingInfo", b =>
                {
                    b.Property<int>("BuildingInfoId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Address");

                    b.Property<int>("BuildYear");

                    b.Property<string>("Discriminator")
                        .IsRequired();

                    b.HasKey("BuildingInfoId");

                    b.ToTable("BuildingInfos");

                    b.HasDiscriminator<string>("Discriminator").HasValue("BuildingInfo");
                });

            modelBuilder.Entity("SpbBuildingAgeMaps.DataModel.BuildingInfoWithLocation", b =>
                {
                    b.HasBaseType("SpbBuildingAgeMaps.DataModel.BuildingInfo");


                    b.ToTable("BuildingInfoWithLocation");

                    b.HasDiscriminator().HasValue("BuildingInfoWithLocation");
                });

            modelBuilder.Entity("SpbBuildingAgeMaps.DataModel.BuildingInfoWithPoligon", b =>
                {
                    b.HasBaseType("SpbBuildingAgeMaps.DataModel.BuildingInfo");

                    b.Property<byte[]>("GeometryData");

                    b.ToTable("BuildingInfoWithPoligon");

                    b.HasDiscriminator().HasValue("BuildingInfoWithPoligon");
                });
#pragma warning restore 612, 618
        }
    }
}
