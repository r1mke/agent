using AiAgents.BeeHiveAgent.Application.Interfaces;
using AiAgents.BeeHiveAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.BeeHiveAgent.Infrastructure.Persistence;

public class BeeHiveAgentDbContext : DbContext, IAppDbContext
{
    // Konstruktor koji prima opcije (connection string ide ovdje)
    public BeeHiveAgentDbContext(DbContextOptions<BeeHiveAgentDbContext> options)
        : base(options)
    {
    }

    // Tabele u bazi
    public DbSet<HiveImageSample> ImageSamples { get; set; }
    public DbSet<Prediction> Predictions { get; set; }
    public DbSet<SystemSettings> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- FIX: Eksplicitno definiranje veze i stranog ključa ---
        // Ovim kažemo EF Core-u da poveže Prediction sa HiveImageSample preko kolone SampleId
        modelBuilder.Entity<HiveImageSample>()
            .HasMany(s => s.Predictions)
            .WithOne()
            .HasForeignKey(p => p.SampleId);

        // CONFIG 1: SystemSettings je Singleton (uvijek ID=1)
        // Ovo osigurava da u bazi uvijek imamo jedan red za postavke
        modelBuilder.Entity<SystemSettings>().HasData(
            new SystemSettings
            {
                Id = 1,
                NewGoldSinceLastTrain = 0,
                IsRetrainEnabled = true,
                RetrainGoldThreshold = 50,
                AutoThresholdHigh = 0.90f,
                AutoThresholdLow = 0.15f
            }
        );

        // CONFIG 2: Podešavanje preciznosti za float (SQL 'real')
        modelBuilder.Entity<Prediction>()
            .Property(p => p.Score)
            .HasColumnType("real");
    }
}