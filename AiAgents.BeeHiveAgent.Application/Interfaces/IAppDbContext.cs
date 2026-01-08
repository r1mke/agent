using AiAgents.BeeHiveAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.BeeHiveAgent.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<HiveImageSample> ImageSamples { get; }
    DbSet<Prediction> Predictions { get; }
    DbSet<SystemSettings> Settings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}