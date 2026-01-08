using AiAgents.BeeHiveAgent.Domain.Entities;
using AiAgents.BeeHiveAgent.Domain.Enums;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AiAgents.BeeHiveAgent.Infrastructure.Persistence;

/// <summary>
/// Klasa koja mapira kolone iz bee_data.csv
/// </summary>
public class BeeCsvRecord
{
    public string file { get; set; } = string.Empty;
    public string pollen_carrying { get; set; } = string.Empty;
}

/// <summary>
/// Servis za početno punjenje baze podataka iz CSV dataseta.
/// Koristi OVERSAMPLING strategiju za balansiranje nebalansiranih klasa.
/// </summary>
public class DatabaseSeeder
{
    private readonly BeeHiveAgentDbContext _db;

    public DatabaseSeeder(BeeHiveAgentDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(string datasetPath)
    {
        // 1. Ako baza već ima podatke, ne radimo ništa
        if (await _db.ImageSamples.AnyAsync()) return;

        var csvPath = Path.Combine(datasetPath, "bee_data.csv");
        var imagesPath = Path.Combine(datasetPath, "bee_imgs");

        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"❌ GREŠKA SEEDER: Nema CSV fajla na putanji: {csvPath}");
            return;
        }

        Console.WriteLine("═══════════════════════════════════════════════════");
        Console.WriteLine("📂 DATABASE SEEDER: Učitavam dataset...");
        Console.WriteLine("═══════════════════════════════════════════════════");

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<BeeCsvRecord>().ToList();

        // Liste za svaku klasu
        var pollenImages = new List<(string path, BeeCsvRecord record)>();
        var noPollenImages = new List<(string path, BeeCsvRecord record)>();

        foreach (var r in records)
        {
            var fullImgPath = Path.Combine(imagesPath, r.file);

            // Preskoči ako slika ne postoji
            if (!File.Exists(fullImgPath)) continue;

            var rawValue = r.pollen_carrying?.Trim().ToUpper();
            bool hasPollen = rawValue == "TRUE" || rawValue == "1" || rawValue == "YES";

            if (hasPollen)
                pollenImages.Add((fullImgPath, r));
            else
                noPollenImages.Add((fullImgPath, r));
        }

        Console.WriteLine($"📊 ORIGINALNI DATASET:");
        Console.WriteLine($"   -> Pollen slike: {pollenImages.Count}");
        Console.WriteLine($"   -> NoPollen slike: {noPollenImages.Count}");

        // ═══════════════════════════════════════════════════════════════
        // OVERSAMPLING STRATEGIJA
        // ═══════════════════════════════════════════════════════════════
        // Problem: Imamo 18 Pollen vs 5000+ NoPollen
        // Rješenje: Dupliciramo Pollen slike da dobijemo balans

        const int TARGET_PER_CLASS = 500; // Cilj: 500 slika po klasi

        var finalTrainingSet = new List<HiveImageSample>();

        // --- POLLEN KLASA (sa oversamplingom) ---
        if (pollenImages.Count > 0)
        {
            int oversampleFactor = (int)Math.Ceiling((double)TARGET_PER_CLASS / pollenImages.Count);

            Console.WriteLine($"");
            Console.WriteLine($"🔄 OVERSAMPLING POLLEN KLASE:");
            Console.WriteLine($"   -> Original: {pollenImages.Count} slika");
            Console.WriteLine($"   -> Faktor: {oversampleFactor}x");

            int addedPollen = 0;
            for (int i = 0; i < oversampleFactor && addedPollen < TARGET_PER_CLASS; i++)
            {
                foreach (var (path, record) in pollenImages)
                {
                    if (addedPollen >= TARGET_PER_CLASS) break;

                    finalTrainingSet.Add(new HiveImageSample
                    {
                        Id = Guid.NewGuid(),
                        HiveId = Guid.NewGuid(),
                        ImagePath = path,
                        CapturedAt = DateTime.UtcNow,
                        Status = SampleStatus.Reviewed,
                        TaskType = TaskType.Pollen,
                        Label = "Pollen"
                    });
                    addedPollen++;
                }
            }
            Console.WriteLine($"   -> Rezultat: {addedPollen} samplea");
        }
        else
        {
            Console.WriteLine("⚠️ UPOZORENJE: Nema Pollen slika u datasetu!");
        }

        // --- NOPOLLEN KLASA (uzimamo uzorak) ---
        if (noPollenImages.Count > 0)
        {
            Console.WriteLine($"");
            Console.WriteLine($"📉 SAMPLING NOPOLLEN KLASE:");
            Console.WriteLine($"   -> Original: {noPollenImages.Count} slika");

            // Nasumično promiješaj i uzmi TARGET_PER_CLASS
            var random = new Random(42); // Fixed seed za reproducibilnost
            var sampledNoPollen = noPollenImages
                .OrderBy(_ => random.Next())
                .Take(TARGET_PER_CLASS)
                .ToList();

            foreach (var (path, record) in sampledNoPollen)
            {
                finalTrainingSet.Add(new HiveImageSample
                {
                    Id = Guid.NewGuid(),
                    HiveId = Guid.NewGuid(),
                    ImagePath = path,
                    CapturedAt = DateTime.UtcNow,
                    Status = SampleStatus.Reviewed,
                    TaskType = TaskType.Pollen,
                    Label = "NoPollen"
                });
            }
            Console.WriteLine($"   -> Rezultat: {sampledNoPollen.Count} samplea");
        }

        // --- STATISTIKA FINALNOG DATASETA ---
        var finalPollen = finalTrainingSet.Count(s => s.Label == "Pollen");
        var finalNoPollen = finalTrainingSet.Count(s => s.Label == "NoPollen");

        Console.WriteLine($"");
        Console.WriteLine($"═══════════════════════════════════════════════════");
        Console.WriteLine($"✅ FINALNI BALANSIRANI DATASET:");
        Console.WriteLine($"   -> Pollen: {finalPollen} samplea");
        Console.WriteLine($"   -> NoPollen: {finalNoPollen} samplea");
        Console.WriteLine($"   -> UKUPNO: {finalTrainingSet.Count} samplea");
        Console.WriteLine($"   -> Balans: {(double)finalPollen / finalTrainingSet.Count:P1} / {(double)finalNoPollen / finalTrainingSet.Count:P1}");
        Console.WriteLine($"═══════════════════════════════════════════════════");

        // Sigurnosna provjera
        if (finalPollen == 0 || finalNoPollen == 0)
        {
            Console.WriteLine("🛑 KRITIČNA GREŠKA: Fali jedna klasa! Trening neće uspjeti.");
            return;
        }

        // Ubaci u bazu
        await _db.ImageSamples.AddRangeAsync(finalTrainingSet);

        // Postavi trigger za RetrainAgent (samo ako model NE postoji)
        var settings = await _db.Settings.FirstOrDefaultAsync();
        if (settings != null)
        {
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModels", "bee_model.zip");

            if (!File.Exists(modelPath))
            {
                // Model ne postoji - pokreni trening
                settings.NewGoldSinceLastTrain = finalTrainingSet.Count;
                Console.WriteLine($"🔔 Model ne postoji - trigger za trening postavljen!");
                Console.WriteLine($"   -> NewGoldSinceLastTrain = {settings.NewGoldSinceLastTrain}");
            }
            else
            {
                // Model već postoji - preskoči trening
                settings.NewGoldSinceLastTrain = 0;
                Console.WriteLine($"✅ Model već postoji ({modelPath})");
                Console.WriteLine($"   -> Preskačem trening, koristim postojeći model");
            }
        }

        await _db.SaveChangesAsync();
        Console.WriteLine($"💾 Podaci uspješno spašeni u bazu!");
    }
}