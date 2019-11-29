﻿using Microsoft.EntityFrameworkCore;
using WindesHeartSDK.Models;

namespace WindesHeartApp.Data
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Heartrate> Heartrates { get; set; }
        public DbSet<StepInfo> Steps { get; set; }

        private readonly string _databasePath;

        public DatabaseContext(string databasePath)
        {
            _databasePath = databasePath;
            //Database.EnsureDeleted();

            Database.EnsureCreated();

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={_databasePath}");
        }
    }
}
