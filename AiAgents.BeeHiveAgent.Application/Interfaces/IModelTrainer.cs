using AiAgents.BeeHiveAgent.Domain.Entities;

namespace AiAgents.BeeHiveAgent.Application.Interfaces;

/// <summary>
/// Interface za servis koji trenira ML model.
/// Omogućava dependency injection i lakše testiranje.
/// </summary>
public interface IModelTrainer
{
    /// <summary>
    /// Trenira novi ML model na osnovu gold podataka.
    /// </summary>
    /// <param name="goldSamples">Lista slika sa potvrđenim labelama</param>
    /// <returns>Verzija novog modela (npr. "v1.0", "v2.0")</returns>
    string TrainModel(List<HiveImageSample> goldSamples);

    /// <summary>
    /// Provjerava da li postoji istrenirani model.
    /// </summary>
    bool ModelExists();
}