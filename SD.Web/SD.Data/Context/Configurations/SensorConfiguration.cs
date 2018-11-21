﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SD.Data.Models;

namespace SD.Data.Context.Configurations
{
    internal class SensorConfiguration : IEntityTypeConfiguration<Sensor>
    {
        public void Configure(EntityTypeBuilder<Sensor> builder)
        {
            builder.ToTable("Sensors");

            builder.HasIndex(s => s.SensorId).IsUnique(true);

            //builder.HasMany(s => s.SensorDatas)
            //    .WithOne(sd => sd.Sensor)
            //    .HasForeignKey(sd => sd.SensorId)
            //    .HasPrincipalKey(s => s.SensorId);
        }
    }
}