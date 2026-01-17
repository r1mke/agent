using AiAgents.BeeHiveAgent.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.BeeHiveAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAppDbContext _db;

    public AdminController(IAppDbContext db)
    {
        _db = db;
    }


    [HttpPost("trigger-retrain")]
    public async Task<IActionResult> TriggerRetrain()
    {
        var settings = await _db.Settings.FirstOrDefaultAsync();

        if (settings == null)
        {
            return NotFound("Settings not found in database.");
        }


        var goldCount = await _db.ImageSamples
            .CountAsync(s => s.Status == Domain.Enums.SampleStatus.Reviewed);

        if (goldCount == 0)
        {
            return BadRequest("No gold samples in database. Upload and review some images first.");
        }


        settings.NewGoldSinceLastTrain = goldCount;
        await _db.SaveChangesAsync(CancellationToken.None);

        return Ok(new
        {
            Message = "Retrain triggered!",
            GoldSamples = goldCount,
            Threshold = settings.RetrainGoldThreshold,
            Note = "RetrainWorker will pick this up within 10 seconds."
        });
    }


    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var settings = await _db.Settings.FirstOrDefaultAsync();
        var totalSamples = await _db.ImageSamples.CountAsync();
        var goldSamples = await _db.ImageSamples
            .CountAsync(s => s.Status == Domain.Enums.SampleStatus.Reviewed);
        var pendingReview = await _db.ImageSamples
            .CountAsync(s => s.Status == Domain.Enums.SampleStatus.PendingReview);
        var queued = await _db.ImageSamples
            .CountAsync(s => s.Status == Domain.Enums.SampleStatus.Queued);

        var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModels", "bee_model.zip");
        var modelExists = System.IO.File.Exists(modelPath);

        return Ok(new
        {
            ModelStatus = modelExists ? "✅ Ready" : "❌ Not trained",
            ModelPath = modelPath,
            Database = new
            {
                TotalSamples = totalSamples,
                GoldSamples = goldSamples,
                PendingReview = pendingReview,
                Queued = queued
            },
            Training = new
            {
                NewGoldSinceLastTrain = settings?.NewGoldSinceLastTrain ?? 0,
                RetrainThreshold = settings?.RetrainGoldThreshold ?? 50,
                WillTrainOnNextCycle = (settings?.NewGoldSinceLastTrain ?? 0) >= (settings?.RetrainGoldThreshold ?? 50)
            }
        });
    }


    [HttpDelete("reset-database")]
    public async Task<IActionResult> ResetDatabase()
    {

        var predictions = await _db.Predictions.ToListAsync();
        _db.Predictions.RemoveRange(predictions);


        var samples = await _db.ImageSamples.ToListAsync();
        _db.ImageSamples.RemoveRange(samples);


        var settings = await _db.Settings.FirstOrDefaultAsync();
        if (settings != null)
        {
            settings.NewGoldSinceLastTrain = 0;
        }

        await _db.SaveChangesAsync(CancellationToken.None);

        return Ok(new
        {
            Message = "Database reset complete!",
            DeletedPredictions = predictions.Count,
            DeletedSamples = samples.Count
        });
    }
}