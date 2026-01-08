using AiAgents.BeeHiveAgent.Application.Interfaces;
using AiAgents.BeeHiveAgent.Application.Runners;
using AiAgents.BeeHiveAgent.Application.Services;
using AiAgents.BeeHiveAgent.Infrastructure.ML;
using AiAgents.BeeHiveAgent.Infrastructure.Persistence;
using AiAgents.BeeHiveAgent.Infrastructure.Services;
using AiAgents.BeeHiveAgent.Web.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
// 1. REGISTRACIJA SERVISA (Dependency Injection)
// ═══════════════════════════════════════════════════════════════════════════

// --- BAZA PODATAKA ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=BeeHiveVisionDb;Trusted_Connection=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<BeeHiveAgentDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IAppDbContext>(provider =>
    provider.GetRequiredService<BeeHiveAgentDbContext>());

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- ML KOMPONENTE ---
builder.Services.AddSingleton<IBeeImageClassifier, MLNetBeeClassifier>();
builder.Services.AddSingleton<TrainingService>();
builder.Services.AddSingleton<IModelTrainer>(provider =>
    provider.GetRequiredService<TrainingService>());

// --- APPLICATION LOGIKA ---
builder.Services.AddSingleton<ScoringPolicy>();
builder.Services.AddScoped<ScoringAgentRunner>();
builder.Services.AddScoped<RetrainAgentRunner>();

// --- INFRASTRUCTURE ---
builder.Services.AddTransient<DatabaseSeeder>();

// --- BACKGROUND WORKERS ---
builder.Services.AddHostedService<ScoringWorker>();
builder.Services.AddHostedService<RetrainWorker>();

// --- API ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════════════
// 2. INICIJALIZACIJA (Kreiranje foldera, seedanje baze)
// ═══════════════════════════════════════════════════════════════════════════

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<BeeHiveAgentDbContext>();

    db.Database.EnsureCreated();

    var uploadsFolder = Path.Combine(app.Environment.ContentRootPath, "UserUploads");
    var modelsFolder = Path.Combine(app.Environment.ContentRootPath, "MLModels");

    Directory.CreateDirectory(uploadsFolder);
    Directory.CreateDirectory(modelsFolder);

    Console.WriteLine("═══════════════════════════════════════════════════");
    Console.WriteLine("🐝 BEE HIVE VISION AGENT - POKRETANJE");
    Console.WriteLine("═══════════════════════════════════════════════════");
    Console.WriteLine($"📁 Uploads folder: {uploadsFolder}");

    // --- SEEDANJE PODATAKA ---
    var datasetPath = Path.Combine(app.Environment.ContentRootPath, "Datasets");
    if (Directory.Exists(datasetPath))
    {
        try
        {
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            seeder.SeedAsync(datasetPath).Wait();
            Console.WriteLine("✅ Dataset uspješno učitan u bazu!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Greška pri seedanju: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine($"⚠️ Dataset folder ne postoji: {datasetPath}");
    }

    var sampleCount = db.ImageSamples.Count();
    var settings = db.Settings.FirstOrDefault();
    var modelExists = File.Exists(Path.Combine(app.Environment.ContentRootPath, "MLModels", "bee_model.zip"));

    Console.WriteLine($"📊 Stanje: Slika={sampleCount}, Gold={settings?.NewGoldSinceLastTrain ?? 0}, Model={(modelExists ? "✅" : "❌")}");
    Console.WriteLine("═══════════════════════════════════════════════════");
}

// ═══════════════════════════════════════════════════════════════════════════
// 3. HTTP PIPELINE
// ═══════════════════════════════════════════════════════════════════════════

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- CORS AKTIVACIJA ---
app.UseCors("AllowAngular");

// --- STATIC FILES ---
// Uploadovane slike: http://localhost:5036/images/naziv.png
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "UserUploads")),
    RequestPath = "/images"
});

// Dataset slike: http://localhost:5036/dataset/017_038.png
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Datasets", "bee_imgs")),
    RequestPath = "/dataset"
});

app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🚀 Server spreman za Angular!");
app.Run();